using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Models; 

namespace Client.Models
{
    public class FeedingEvent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public int TotalGranules { get; set; } = 0;
        public float Intensity { get; set; } = 0;
        public bool IsScheduled { get; set; } = false;
        public string Status { get; set; } = "active";

        public User User { get; set; } = null!;
        public ICollection<EventComment> Comments { get; set; } = new List<EventComment>();
        public ICollection<FeedingStats> Stats { get; set; } = new List<FeedingStats>();
    }
}
