using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Models;

namespace Client.Models
{
    public class FeedingSchedule
    {
        public int Id { get; set; }
        public int SettingsId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string DaysOfWeek { get; set; } = "1,2,3,4,5,6,7";

        public Settings Settings { get; set; } = null!;
    }
}
