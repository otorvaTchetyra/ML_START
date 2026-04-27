using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class WarningLog
    {
        public int Id { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
    }
}
