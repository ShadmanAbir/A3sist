using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.Designer.Models
{
    public class ProjectAnalysis
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<Component> Components { get; set; } = new();
        public List<Dependency> Dependencies { get; set; } = new();
    }
}
