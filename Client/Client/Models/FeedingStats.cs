using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Models;

namespace Client.Models
{
    public class FeedingStats
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public DateOnly PeriodDate { get; set; }
        public byte PeriodHour { get; set; }
        public int GranulesSum { get; set; } = 0;
        public float AvgIntensity { get; set; } = 0;

        public FeedingEvent FeedingEvent { get; set; } = null!;
    }
}
