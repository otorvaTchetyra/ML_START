using System.Text.Json.Serialization;

namespace Client.Models;

public class ServerSettingsResponse
{
    [JsonPropertyName("granule_threshold")]
    public int GranuleThreshold { get; set; } = 50;

    [JsonPropertyName("model_confidence")]
    public float ModelConfidence { get; set; } = 0.3f;

    [JsonPropertyName("model_iou")]
    public float ModelIou { get; set; } = 0.45f;

    [JsonPropertyName("frame_skip")]
    public int FrameSkip { get; set; } = 2;
}
