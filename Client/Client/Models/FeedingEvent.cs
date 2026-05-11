using System;

namespace Client.Models
{
    public class FeedingEvent
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int GranuleCount { get; set; }
        public float IntensityPerSec { get; set; }
        public float IntensityPerMin { get; set; }
        public bool IsOutOfSchedule { get; set; }
        public bool ThresholdExceeded { get; set; }
        public string? Comment { get; set; }
        public StreamFrame? StreamFrame { get; set; }

        public bool HasComment => !string.IsNullOrEmpty(Comment);
        public string StatusColor => IsOutOfSchedule ? "Orange" : "Green";
    }
}
