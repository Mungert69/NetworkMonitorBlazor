=======
# NetworkMonitorBlazor

NetworkMonitorBlazor is a Blazor-based web application that provides a real-time, interactive chat interface for monitoring and managing network hosts using Large Language Models (LLMs). It features a modern chat UI, audio transcription, WebSocket-based communication, and session history management.

## Features

- **Chat Assistant**: Interact with an AI assistant powered by pluggable LLM backends (e.g., TurboLLM, HugLLM, TestLLM).
- **Audio Transcription**: Record and transcribe audio messages using browser APIs and a backend transcription service.
- **WebSocket Communication**: Real-time, bidirectional communication with the LLM server for streaming responses and control messages.
- **Session Management**: View, select, and delete previous chat sessions. Sessions are persisted and grouped by date.
- **Notifications**: In-app notifications for info, warnings, errors, and success messages.
- **Host Management**: View and interact with network host data via function calls and dynamic link rendering.
- **Markdown Rendering**: Rich message formatting with support for Markdown, including code blocks, tables, and more.
- **Responsive UI**: Expandable chat window, history popper, and accessibility features.

## Project Structure

- `Components/Chat.razor` - Main chat UI and logic, including message rendering, audio controls, and session actions.
- `Components/ChatStateService.cs` - Manages chat state, notifications, and session persistence.
- `Components/WebSocketService.cs` - Handles WebSocket connections, message processing, and reconnection logic.
- `Components/AudioService.cs` - Manages audio playback, recording, and transcription via JS interop and HTTP APIs.
- `Components/LLMService.cs` - Provides LLM backend configuration and type management.
- `Components/Models/` - Data models for chat history, messages, notifications, and host links.
- `Components/MarkdownRenderer.razor.cs` - Converts Markdown-formatted text to HTML for display in chat.

## How It Works

1. **Initialization**: On load, the chat component initializes the chat state and establishes a WebSocket connection to the LLM server.
2. **Chatting**: Users can send text or audio messages. Audio is transcribed before being sent.
3. **LLM Responses**: The LLM server streams responses, which are rendered in real-time with Markdown formatting.
4. **Session History**: All chat sessions are saved and can be revisited or deleted from the history popper.
5. **Host Data**: The assistant can provide and interact with network host data via function calls.

## Getting Started

1. **Prerequisites**: .NET 9+ SDK, Node.js (for JS interop), and a configured NetworkMonitorLLM backend server.
2. **Build and Run**:
   ```bash
   dotnet build
   dotnet run
   ```
