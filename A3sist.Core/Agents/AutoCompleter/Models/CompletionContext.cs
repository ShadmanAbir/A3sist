using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.AutoCompleter.Models
{
    public class CompletionContext
    {
        public string Code { get; set; }
        public int CursorPosition { get; set; }
        public string Language { get; set; }
        public string FilePath { get; set; }
        public List<string> ExistingImports { get; set; } = new List<string>();
    }
}
