using System;

namespace Client.Models
{
    public class AppLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}