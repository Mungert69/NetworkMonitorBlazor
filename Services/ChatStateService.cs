using NetworkMonitorBlazor.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace NetworkMonitorBlazor.Services
{
    public class ChatStateService
    {
        // Audio and UI state
        public bool IsMuted { get; set; } = true;
        public bool IsExpanded { get; set; } = false;
        public bool IsDrawerOpen { get; set; } = false;
        public bool AutoScrollEnabled { get; set; } = true;

        // Processing and loading states
        public bool IsReady { get; set; } = true;
        public int LoadCount { get; set; } = 0;
        public string LoadWarning { get; set; } = "";
        public bool IsProcessing { get; set; } = false;
        public bool IsCallingFunction { get; set; } = false;
        public bool IsLLMBusy { get; set; } = false;
        public bool IsToggleDisabled { get; set; } = false;

        // Message and feedback states
        public string ThinkingDots { get; set; } = "";
        public string CallingFunctionMessage { get; set; } = "Calling function...";
        public bool ShowHelpMessage { get; set; } = false;
        public string HelpMessage { get; set; } = "";
        public string CurrentMessage { get; set; } = "";
        public string LLMFeedback { get; set; } = "";
        public bool IsDashboard { get; set; }
        public ChatMessage Message { get; set; } = new ChatMessage 
        { 
            Info = "init", 
            Success = false, 
            Text = "Internal Error" 
        };

        // Data states
        public List<ChatHistory> Histories { get; set; } = new List<ChatHistory>();
        public List<HostLink> LinkData { get; set; } = new List<HostLink>();
        public string LLMRunnerType { get; set; } = "TurboLLM";
        public bool IsHoveringMessages { get; set; } = false;
        public bool IsInputFocused { get; set; } = false;

        // Session management
        public string SessionId { get; set; }
        public string LLMRunnerTypeRef { get; set; }
        public string OpenMessage { get; set; }
        public bool AutoClickedRef { get; set; } = false;

        private readonly IJSRuntime _jsRuntime;

        public ChatStateService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }


public async Task Initialize(string initRunnerType)
{
    LLMRunnerType = initRunnerType;
    LLMRunnerTypeRef = initRunnerType;
    SessionId = await CreateNewSession();
    OnChange?.Invoke();
}
  public async Task ClearSession()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionId");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionTimestamp");
        SessionId = string.Empty;
        OnChange?.Invoke();
    }
private async Task<string> CreateNewSession()
{
    var newSessionId = Guid.NewGuid().ToString();
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionId", newSessionId);
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionTimestamp", DateTime.Now.Ticks.ToString());
    return newSessionId;
}

        private async Task<string> GetSessionId()
        {
            // Check if we have a recent session in localStorage
            var storedSessionId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sessionId");
            var storedTimestamp = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sessionTimestamp");
            
            if (!string.IsNullOrEmpty(storedSessionId) && !string.IsNullOrEmpty(storedTimestamp))
            {
                var currentTime = DateTime.Now.Ticks;
                var storedTime = long.Parse(storedTimestamp);
                var oneDayInTicks = TimeSpan.TicksPerDay;
                
                if (currentTime - storedTime <= oneDayInTicks)
                {
                    return storedSessionId;
                }
                
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionId");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "sessionTimestamp");
            }

            // Create new session
            var newSessionId = Guid.NewGuid().ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionId", newSessionId);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "sessionTimestamp", DateTime.Now.Ticks.ToString());
            return newSessionId;
        }

       public event Action? OnChange = null; // Mark as nullable


        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
