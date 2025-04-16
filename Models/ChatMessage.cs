// Models/ChatMessage.cs
namespace NetworkMonitorBlazor.Models
{
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;  // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}