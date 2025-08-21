using A3sist.Agents.Designer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.Designer.Models
{
    public class DocumentAnalysis
    {
        public string DocumentName { get; set; } = string.Empty;
        public List<Component> Components { get; set; } = new();
        public List<Dependency> Dependencies { get; set; } = new();
    }
}
