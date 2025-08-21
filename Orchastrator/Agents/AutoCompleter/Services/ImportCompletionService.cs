using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents.AutoCompleter.Models;
using Microsoft.IO;

namespace A3sist.Orchastrator.Agents.AutoCompleter.Services
{
    public class ImportCompletionService
    {
        private readonly Dictionary<string, List<ImportItem>> _languageImports = new Dictionary<string, List<ImportItem>>();

        public async Task InitializeAsync()
        {
            // Initialize language-specific imports
            InitializeLanguageImports();
        }

        private void InitializeLanguageImports()
        {
            // C# imports
            _languageImports["C#"] = new List<ImportItem>
            {
                new ImportItem
                {
                    Namespace = "System",
                    Description = "Basic system functionality",
                    CommonTypes = new List<string> { "Console", "Math", "String", "DateTime" }
                },
                new ImportItem
                {
                    Namespace = "System.Collections.Generic",
                    Description = "Generic collections",
                    CommonTypes = new List<string> { "List", "Dictionary", "Queue", "Stack" }
                },
                new ImportItem
                {
                    Namespace = "System.Linq",
                    Description = "Language Integrated Query",
                    CommonTypes = new List<string> { "Enumerable", "Queryable" }
                },
                new ImportItem
                {
                    Namespace = "System.Threading.Tasks",
                    Description = "Asynchronous programming",
                    CommonTypes = new List<string> { "Task", "Task<T>", "ValueTask" }
                },
                new ImportItem
                {
                    Namespace = "System.IO",
                    Description = "Input and Output operations",
                    CommonTypes = new List<string> { "File", "Directory", "Stream", "Path" }
                },
                new ImportItem
                {
                    Namespace = "System.Text",
                    Description = "Text encoding and manipulation",
                    CommonTypes = new List<string> { "StringBuilder", "Encoding" }
                },
                new ImportItem
                {
                    Namespace = "System.Text.Json",
                    Description = "JSON serialization and deserialization",
                    CommonTypes = new List<string> { "JsonSerializer", "JsonDocument" }
                },
                new ImportItem
                {
                    Namespace = "System.Net.Http",
                    Description = "HTTP client functionality",
                    CommonTypes = new List<string> { "HttpClient", "HttpResponseMessage" }
                }
            };

            // JavaScript imports
            _languageImports["JavaScript"] = new List<ImportItem>
            {
                new ImportItem
                {
                    Namespace = "react",
                    Description = "React library",
                    CommonTypes = new List<string> { "Component", "useState", "useEffect" }
                },
                new ImportItem
                {
                    Namespace = "react-dom",
                    Description = "React DOM library",
                    CommonTypes = new List<string> { "render", "createRoot" }
                },
                new ImportItem
                {
                    Namespace = "redux",
                    Description = "Redux state management",
                    CommonTypes = new List<string> { "createStore", "combineReducers" }
                },
                new ImportItem
                {
                    Namespace = "react-redux",
                    Description = "React-Redux bindings",
                    CommonTypes = new List<string> { "Provider", "connect", "useSelector" }
                },
                new ImportItem
                {
                    Namespace = "axios",
                    Description = "HTTP client",
                    CommonTypes = new List<string> { "axios", "get", "post" }
                },
                new ImportItem
                {
                    Namespace = "lodash",
                    Description = "Utility library",
                    CommonTypes = new List<string> { "map", "filter", "debounce" }
                },
                new ImportItem
                {
                    Namespace = "moment",
                    Description = "Date manipulation library",
                    CommonTypes = new List<string> { "moment", "format", "parse" }
                },
                new ImportItem
                {
                    Namespace = "prop-types",
                    Description = "Runtime type checking",
                    CommonTypes = new List<string> { "PropTypes", "shape", "oneOf" }
                }
            };

            // Python imports
            _languageImports["Python"] = new List<ImportItem>
            {
                new ImportItem
                {
                    Namespace = "os",
                    Description = "Operating system interfaces",
                    CommonTypes = new List<string> { "path", "environ", "system" }
                },
                new ImportItem
                {
                    Namespace = "sys",
                    Description = "System-specific parameters and functions",
                    CommonTypes = new List<string> { "argv", "exit", "stdout" }
                },
                new ImportItem
                {
                    Namespace = "math",
                    Description = "Mathematical functions",
                    CommonTypes = new List<string> { "sqrt", "sin", "cos", "pi" }
                },
                new ImportItem
                {
                    Namespace = "datetime",
                    Description = "Date and time handling",
                    CommonTypes = new List<string> { "datetime", "date", "time" }
                },
                new ImportItem
                {
                    Namespace = "json",
                    Description = "JSON encoding and decoding",
                    CommonTypes = new List<string> { "loads", "dumps", "JSONEncoder" }
                },
                new ImportItem
                {
                    Namespace = "requests",
                    Description = "HTTP requests",
                    CommonTypes = new List<string> { "get", "post", "Response" }
                },
                new ImportItem
                {
                    Namespace = "pandas",
                    Description = "Data analysis library",
                    CommonTypes = new List<string> { "DataFrame", "read_csv", "Series" }
                },
                new ImportItem
                {
                    Namespace = "numpy",
                    Description = "Numerical computing",
                    CommonTypes = new List<string> { "array", "ndarray", "random" }
                }
            };
        }

        public async Task<List<ImportItem>> GetCompletionsAsync(CompletionContext context)
        {
            if (!_languageImports.TryGetValue(context.Language, out var imports))
            {
                return new List<ImportItem>();
            }

            // Filter imports based on context
            var filteredImports = imports
                .Where(i => i.Namespace.Contains(context.Code.Substring(context.CursorPosition - 1)))
                .ToList();

            // Rank imports based on relevance
            var rankedImports = filteredImports
                .OrderByDescending(i => i.Namespace.Length) // Longer matches first
                .ThenBy(i => i.Namespace) // Alphabetical order
                .Take(5) // Return top 5 suggestions
                .ToList();

            return rankedImports;
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _languageImports.Clear();
        }
    }


}