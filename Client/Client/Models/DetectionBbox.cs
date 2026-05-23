namespace Client.Models
{
    public class DetectionBbox
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
        public double confidence { get; set; }

        public string label { get; set; } = "";
        public double? Width { get; set; }
        public double? Height { get; set; }

        public bool HasLabel => !string.IsNullOrEmpty(label);
    }
}
