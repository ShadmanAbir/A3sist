using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.AutoCompleter.Models
{
    public class ImportItem
    {
        public string Namespace { get; set; }
        public string Description { get; set; }
        public List<string> CommonTypes { get; set; } = new List<string>();
    }
}
