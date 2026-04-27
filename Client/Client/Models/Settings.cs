using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Models;

namespace Client.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public int GranuleThreshold { get; set; } = 100;
        public string? AlgoParams { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<FeedingSchedule> Schedules { get; set; } = new List<FeedingSchedule>();
    }
}
