namespace NetworkMonitorBlazor.Models
{
    public class ChatHistory
    {
        public string SessionId { get; set; }= string.Empty;
        public string DisplayName { get; set; }= string.Empty;
        public string LLMType { get; set; }= string.Empty;
        public string Timestamp { get; set; }= string.Empty;
    }
}