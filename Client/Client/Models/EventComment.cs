using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class EventComment
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public FeedingEvent FeedingEvent { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
