using NetworkMonitorBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitorBlazor.Services
{
    public class WebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private readonly ChatStateService _chatState;
        private readonly IJSRuntime _jsRuntime;
        private readonly AudioService _audioService;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string _siteId;
        private readonly ILLMService _llmService;

        public WebSocketService(ChatStateService chatState, IJSRuntime jsRuntime, AudioService audioService, ILLMService llmService)
        {
            _chatState = chatState;
            _jsRuntime = jsRuntime;
            _audioService = audioService;
            _cancellationTokenSource = new CancellationTokenSource();
              _siteId = string.Empty; 
              _llmService=llmService;
        }




     public async Task Initialize(string siteId, string sessionId)
{
    _siteId = siteId;
    CurrentSessionId = sessionId;
    
    // Rest of your initialization code
    await ConnectWebSocket();
    
    // Send initialization message with both IDs
    var timeZone = TimeZoneInfo.Local.Id;
    var sendStr = $"{timeZone},{_chatState.LLMRunnerTypeRef},{sessionId}";
    await Send(sendStr);
}
public async Task<bool> VerifySession()
{
    try
    {
        await Send($"<|VERIFY_SESSION|{CurrentSessionId}|>");
        return true;
    }
    catch
    {
        return false;
    }
}
        private async Task ConnectWebSocket()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                var serverUrl = _llmService.GetLLMServerUrl(_siteId);
                await _webSocket.ConnectAsync(new Uri(serverUrl), _cancellationTokenSource.Token);
                Console.WriteLine($"WebSocket connection established to {serverUrl}");

                // Send initial connection message
                var timeZone = TimeZoneInfo.Local.Id;
                var sendStr = $"{timeZone},{_chatState.LLMRunnerTypeRef},{_chatState.SessionId}";
                await Send(sendStr);
                Console.WriteLine($"Sent opening message to websocket: {sendStr}");

                // Start listening for messages
                _ = Task.Run(ReceiveMessages, _cancellationTokenSource.Token);

                // Start ping interval
                _ = Task.Run(PingInterval, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"WebSocket connection error: {ex}");
                await Reconnect();
            }
        }

        private async Task PingInterval()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_webSocket?.State == WebSocketState.Open)
                    {
                        await Send("");
                        Console.WriteLine("Sent web socket Ping");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Ping error: {ex}");
                }
                await Task.Delay(5000, _cancellationTokenSource.Token);
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024 * 4];
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        await Reconnect();
                        return;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"WebSocket receive error: {ex}");
                    await Reconnect();
                    return;
                }
            }
        }

        private async Task ProcessMessage(string message)
        {
            Console.WriteLine($"Received message: {message}");

            // Handle audio data
            if (message.Contains("</audio>"))
            {
                var parts = message.Split(new[] { "</audio>" }, StringSplitOptions.None);
                var textPart = parts[0]?.Trim() ?? "";
                var audioFile = parts.Length > 1 ? parts[1]?.Trim() : "";

                if (!string.IsNullOrEmpty(textPart))
                {
                    _chatState.LLMFeedback += FilterLLMOutput(textPart);
                }

                if (!string.IsNullOrEmpty(audioFile))
                {
                    await _audioService.PlayAudioSequentially(audioFile);
                }
                return;
            }

            // Handle function data
            if (message.StartsWith("<function-data>") && message.EndsWith("</function-data>"))
            {
                var functionData = message.Substring("<function-data>".Length, message.Length - "<function-data>".Length - "</function-data>".Length);
                var generatedLinkData = ProcessFunctionData(functionData);

                if (generatedLinkData != null)
                {
                    _chatState.LinkData = generatedLinkData;
                    if (generatedLinkData.Count > 1)
                    {
                        _chatState.IsDrawerOpen = true;
                    }
                }
                return;
            }

            // Handle history display data
            if (message.StartsWith("<history-display-name>") && message.EndsWith("</history-display-name>"))
            {
                var historyData = message.Substring("<history-display-name>".Length, message.Length - "<history-display-name>".Length - "</history-display-name>".Length);
                ProcessHistoryDisplayData(historyData);
                return;
            }

            // Handle control messages
            if (message.StartsWith("</llm-error>"))
            {
                _chatState.Message = new ChatMessage
                {
                    Persist = true,
                    Text = message.Substring("</llm-error>".Length),
                    Success = false
                };
                return;
            }
            else if (message.StartsWith("</llm-info>"))
            {
                _chatState.Message = new ChatMessage
                {
                    Info = "",
                    Text = message.Substring("</llm-info>".Length)
                };
                return;
            }
            else if (message.StartsWith("</llm-warning>"))
            {
                _chatState.Message = new ChatMessage
                {
                    Warning = "",
                    Text = message.Substring("</llm-warning>".Length)
                };
                return;
            }
            else if (message.StartsWith("</llm-success>"))
            {
                _chatState.Message = new ChatMessage
                {
                    Success = true,
                    Text = message.Substring("</llm-success>".Length)
                };
                return;
            }
            else if (message == "</llm-ready>")
            {
                _chatState.IsReady = true;
                return;
            }
            else if (message == "</functioncall>")
            {
                _chatState.IsCallingFunction = true;
                return;
            }
            else if (message == "</functioncall-complete>")
            {
                _chatState.IsCallingFunction = false;
                return;
            }
            else if (message == "</llm-busy>")
            {
                _chatState.IsLLMBusy = true;
                return;
            }
            else if (message == "</llm-listening>")
            {
                _chatState.IsLLMBusy = false;
                return;
            }
            else if (message == "<end-of-line>")
            {
                _chatState.IsProcessing = false;
                return;
            }

            // Default case: append to feedback
            _chatState.LLMFeedback += FilterLLMOutput(message);
            _chatState.NotifyStateChanged();
        }

        private string FilterLLMOutput(string text)
        {
            var replacements = new Dictionary<string, string>
            {
                { "<\\|from\\|> user.*\\n<\\|recipient\\|> all.*\\n<\\|content\\|>", "<User:> " },
                { "<\\|from\\|> assistant\\n<\\|recipient\\|> (?!all).*<\\|content\\|>", "<Function Call:>" },
                { "<Assistant:><\\|reserved_special_token_249\\|>", "<Function Call:>" },
                { "<Assistant:><tool_call>", "<Function Call:>" },
                { "<\\|from\\|> assistant\\n<\\|recipient\\|> all\\n<\\|content\\|>", "<Assistant:>" },
                { "<\\|start_header_id\\|>assistant<\\|end_header_id\\|>\\n\\n>>>all\\n", "<Assistant:>" },
                { "<\\|start_header_id\\|>assistant<\\|end_header_id\\|>\\n\\n", "<Assistant:>" },
                { "<\\|im_start\\|>assistant\\n", "<Assistant:>" },
                { "<\\|im_start\\|>assistant<\\|im_sep\\|>\\n", "<Assistant:>" },
                { "<\\|assistant\\|>\\n", "<Assistant:>" },
                { "<\\|from\\|> (?!user|assistant).*<\\|recipient\\|> all.*\\n<\\|content\\|>", "<Function Response:> " },
                { "<\\|stop\\|>", "\n" },
                { "<\\|eot_id\\|>", "\n" },
                { "<\\|eom_id\\|>", "\n" },
                { "<\\|im_end\\|>", "\n" },
                { "<\\|end\\|>", "\n" }
            };

            var filteredText = text;
            foreach (var replacement in replacements)
            {
                filteredText = System.Text.RegularExpressions.Regex.Replace(filteredText, replacement.Key, replacement.Value, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return filteredText;
        }

        private List<HostLink> ProcessFunctionData(string functionData)
        {
            if (!_chatState.IsDashboard) return null;

            try
            {
                var jsonData = System.Text.Json.JsonSerializer.Deserialize<FunctionData>(functionData);
                if (jsonData == null || string.IsNullOrEmpty(jsonData.Name) || jsonData.DataJson == null)
                {
                    Console.Error.WriteLine("Malformed function data received");
                    return null;
                }

                switch (jsonData.Name)
                {
                    case "get_host_list":
                        return jsonData.DataJson.Select(host => new HostLink
                        {
                            Address = host.Address,
                            UserID = host.UserID,
                            IsHostList = host.UserID != "default",
                            DataSetID = 0,
                            DateStarted = host.DateStarted
                        }).ToList();

                    case "get_host_data":
                        return jsonData.DataJson.Select(host => new HostLink
                        {
                            Address = host.Address,
                            IsHostData = true,
                            DateStarted = host.DateStarted
                        }).ToList();

                    case "add_host":
                    case "edit_host":
                        return jsonData.DataJson.Select(host => new HostLink
                        {
                            Address = host.Address,
                            UserID = host.UserID,
                            IsHostList = host.UserID != "default",
                            DateStarted = host.DateStarted
                        }).ToList();

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing function data: {ex}");
                return null;
            }
        }

        private void ProcessHistoryDisplayData(string historyDisplayData)
        {
            try
            {
                var histories = System.Text.Json.JsonSerializer.Deserialize<List<ChatHistory>>(historyDisplayData);
                if (histories != null)
                {
                    _chatState.Histories = histories;
                    _chatState.NotifyStateChanged();
                }
                else
                {
                    Console.Error.WriteLine("Invalid history data format");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error parsing history display data: {ex}");
            }
        }

        public async Task SendMessage(string message)
        {
            try
            {
                await WaitForWebSocket();
                await Send(message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending message: {ex}");
                await Reconnect();
            }
        }

        public async Task StopLLM()
        {
            await SendMessage("<|STOP_LLM|>");
            Console.WriteLine("Message sent: <|STOP_LLM|>");
        }

        public async Task ResetSessionId()
        {
            await SendMessage("<|REMOVE_SESSION|>");
            Console.WriteLine("Message sent: <|REMOVE_SESSION|>");

            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionTimestamp");

            var newSessionId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionId", newSessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionTimestamp", DateTime.Now.Ticks.ToString());
            _chatState.SessionId = newSessionId;
        }

        private async Task WaitForWebSocket()
        {
            while (_webSocket?.State != WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(100, _cancellationTokenSource.Token);
            }
        }

        private async Task Send(string message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not open");

            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }

        private async Task Reconnect()
        {
            // Wait before reconnecting
            await Task.Delay(5000, _cancellationTokenSource.Token);
            await Initialize(_siteId);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _webSocket?.Dispose();
        }

        private class FunctionData
        {
            public string Name { get; set; }
            public List<HostLink> DataJson { get; set; }
        }
    }
}