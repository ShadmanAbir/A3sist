using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Models
{
    public class AgentStatus
    {
        public string AgentId { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}