namespace Client.Models
{
    public class HealthStatus
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}