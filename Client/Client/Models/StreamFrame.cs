using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace Client.Models
{
    public class StreamFrame
    {
        public int Frame_index { get; set; }
        public Bitmap? Frame { get; set; }
        public double Timestamp { get; set; }
        public int Granule_count { get; set; }
        public float Intensity_per_sec { get; set; }
        public double Intensity_per_min { get; set; }
        public bool Threshold_exceeded { get; set; }
        public bool Out_of_schedule { get; set; }
        public List<DetectionBbox> bboxes { get; set; } = new();
        public int Source_width { get; set; }
        public int Source_height { get; set; }
    }
}
