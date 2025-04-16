// Models/ChatHistory.cs
namespace NetworkMonitorBlazor.Models
{
    public class ChatHistory
    {
        public string Name { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        public long StartUnixTime { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string LLMType { get; set; } = string.Empty;

        // Helper property to convert Unix time to DateTime
        public DateTime StartTime => DateTimeOffset.FromUnixTimeSeconds(StartUnixTime).DateTime;
    }
}