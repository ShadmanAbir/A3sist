using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeAssist.Agents.AutoCompleter;

namespace CodeAssist.Agents.AutoCompleter.Services
{
    public class SnippetCompletionService
    {
        private readonly Dictionary<string, List<Snippet>> _languageSnippets = new Dictionary<string, List<Snippet>>();

        public async Task InitializeAsync()
        {
            // Initialize language-specific snippets
            InitializeLanguageSnippets();
        }

        private void InitializeLanguageSnippets()
        {
            // C# snippets
            _languageSnippets["C#"] = new List<Snippet>
            {
                new Snippet
                {
                    Name = "if-else",
                    Description = "Basic if-else statement",
                    Code = "if (condition)\n{\n\t// Code to execute if condition is true\n}\nelse\n{\n\t// Code to execute if condition is false\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "for-loop",
                    Description = "Basic for loop",
                    Code = "for (int i = 0; i < length; i++)\n{\n\t// Loop body\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "foreach-loop",
                    Description = "Basic foreach loop",
                    Code = "foreach (var item in collection)\n{\n\t// Loop body\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "try-catch",
                    Description = "Basic try-catch block",
                    Code = "try\n{\n\t// Code that might throw an exception\n}\ncatch (Exception ex)\n{\n\t// Handle the exception\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "class",
                    Description = "Basic class definition",
                    Code = "public class ClassName\n{\n\t// Fields\n\n\t// Constructor\n\tpublic ClassName()\n\t{\n\t\t// Initialization code\n\t}\n\n\t// Methods\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "interface",
                    Description = "Basic interface definition",
                    Code = "public interface IInterfaceName\n{\n\t// Method signatures\n\tvoid MethodName();\n\n\t// Properties\n\tstring PropertyName { get; set; }\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "async-method",
                    Description = "Basic async method",
                    Code = "public async Task MethodNameAsync()\n{\n\t// Async code\n}",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "property",
                    Description = "Basic property",
                    Code = "public Type PropertyName { get; set; }",
                    Language = "C#"
                },
                new Snippet
                {
                    Name = "event",
                    Description = "Basic event",
                    Code = "public event EventHandler EventName;\n\nprotected virtual void OnEventName()\n{\n\tEventName?.Invoke(this, EventArgs.Empty);\n}",
                    Language = "C#"
                }
            };

            // JavaScript snippets
            _languageSnippets["JavaScript"] = new List<Snippet>
            {
                new Snippet
                {
                    Name = "if-else",
                    Description = "Basic if-else statement",
                    Code = "if (condition) {\n\t// Code to execute if condition is true\n} else {\n\t// Code to execute if condition is false\n}",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "for-loop",
                    Description = "Basic for loop",
                    Code = "for (let i = 0; i < array.length; i++) {\n\t// Loop body\n}",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "forEach-loop",
                    Description = "Basic forEach loop",
                    Code = "array.forEach(element => {\n\t// Loop body\n});",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "try-catch",
                    Description = "Basic try-catch block",
                    Code = "try {\n\t// Code that might throw an exception\n} catch (error) {\n\t// Handle the exception\n}",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "function",
                    Description = "Basic function",
                    Code = "function functionName() {\n\t// Function body\n}",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "arrow-function",
                    Description = "Basic arrow function",
                    Code = "const functionName = () => {\n\t// Function body\n};",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "class",
                    Description = "Basic class definition",
                    Code = "class ClassName {\n\t// Constructor\n\tconstructor() {\n\t\t// Initialization code\n\t}\n\n\t// Methods\n\tmethodName() {\n\t\t// Method body\n\t}\n}",
                    Language = "JavaScript"
                },
                new Snippet
                {
                    Name = "event-listener",
                    Description = "Basic event listener",
                    Code = "element.addEventListener('event', (event) => {\n\t// Event handler code\n});",
                    Language = "JavaScript"
                }
            };

            // Python snippets
            _languageSnippets["Python"] = new List<Snippet>
            {
                new Snippet
                {
                    Name = "if-else",
                    Description = "Basic if-else statement",
                    Code = "if condition:\n\t# Code to execute if condition is true\nelse:\n\t# Code to execute if condition is false",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "for-loop",
                    Description = "Basic for loop",
                    Code = "for item in iterable:\n\t# Loop body",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "while-loop",
                    Description = "Basic while loop",
                    Code = "while condition:\n\t# Loop body",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "try-except",
                    Description = "Basic try-except block",
                    Code = "try:\n\t# Code that might raise an exception\nexcept Exception as e:\n\t# Handle the exception",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "function",
                    Description = "Basic function",
                    Code = "def function_name():\n\t# Function body\n\tpass",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "class",
                    Description = "Basic class definition",
                    Code = "class ClassName:\n\t# Constructor\n\tdef __init__(self):\n\t\t# Initialization code\n\t\tpass\n\n\t# Methods\n\tdef method_name(self):\n\t\t# Method body\n\t\tpass",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "list-comprehension",
                    Description = "Basic list comprehension",
                    Code = "[x for x in iterable if condition]",
                    Language = "Python"
                },
                new Snippet
                {
                    Name = "with-statement",
                    Description = "Basic with statement",
                    Code = "with open('file.txt') as f:\n\t# Code to work with the file",
                    Language = "Python"
                }
            };
        }

        public async Task<List<Snippet>> GetCompletionsAsync(CompletionContext context)
        {
            if (!_languageSnippets.TryGetValue(context.Language, out var snippets))
            {
                return new List<Snippet>();
            }

            // Filter snippets based on context
            var filteredSnippets = snippets
                .Where(s => s.Code.Contains(context.Code.Substring(context.CursorPosition - 1), StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Rank snippets based on relevance
            var rankedSnippets = filteredSnippets
                .OrderByDescending(s => s.Code.Length) // Longer matches first
                .ThenBy(s => s.Name) // Alphabetical order
                .Take(5) // Return top 5 suggestions
                .ToList();

            return rankedSnippets;
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _languageSnippets.Clear();
        }
    }

    public class Snippet
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
    }
}