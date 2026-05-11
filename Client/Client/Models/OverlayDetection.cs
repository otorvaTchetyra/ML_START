namespace Client.Models
{
    public class OverlayDetection
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
