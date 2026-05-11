namespace Client.Models
{
    public class StreamStatus
    {
        public bool IsRunning { get; set; }
        public int TotalGranules { get; set; }
        public int FramesProcessed { get; set; }
    }
}
