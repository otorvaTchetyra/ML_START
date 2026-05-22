using System;
using System.Text.Json.Serialization;

namespace Client.Models
{
    public class FeedingEvent
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("granule_count")]
        public int GranuleCount { get; set; }

        [JsonPropertyName("intensity_per_sec")]
        public float IntensityPerSec { get; set; }

        [JsonPropertyName("intensity_per_min")]
        public float IntensityPerMin { get; set; }

        [JsonPropertyName("is_out_of_schedule")]
        public bool IsOutOfSchedule { get; set; }

        [JsonPropertyName("threshold_exceeded")]
        public bool ThresholdExceeded { get; set; }

        public string? Comment { get; set; }
        public StreamFrame? StreamFrame { get; set; }

        public bool HasComment => !string.IsNullOrEmpty(Comment);
        public string StatusColor => IsOutOfSchedule ? "Orange" : "Green";
    }
}
