using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace A3sist.Orchastrator.Agents.CSharp.Services
{
    /// <summary>
    /// Provides comprehensive XAML validation and manipulation capabilities
    /// </summary>
    public class XamlValidator : IDisposable
    {
        private bool _disposed = false;
        private XmlSchemaSet _schemaSet;
        private readonly List<string> _validationErrors;

        public XamlValidator()
        {
            _validationErrors = new List<string>();
        }

        /// <summary>
        /// Initializes the XAML validator asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Initialize XML schema set for XAML validation
            _schemaSet = new XmlSchemaSet();
            
            // In a real implementation, you would load XAML schemas here
            // For now, we'll use basic XML validation
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Validates XAML content asynchronously with comprehensive error reporting
        /// </summary>
        /// <param name="xaml">The XAML content to validate</param>
        /// <returns>Validation results with detailed information</returns>
        public async Task<string> ValidateXamlAsync(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
            {
                return "XAML validation error: No XAML content provided";
            }

            var results = new List<string>();
            _validationErrors.Clear();

            try
            {
                // 1. Basic XML structure validation
                var xmlValidation = await ValidateXmlStructureAsync(xaml);
                results.AddRange(xmlValidation);

                // 2. XAML-specific validation
                var xamlValidation = await ValidateXamlStructureAsync(xaml);
                results.AddRange(xamlValidation);

                // 3. Namespace validation
                var namespaceValidation = await ValidateNamespacesAsync(xaml);
                results.AddRange(namespaceValidation);

                // 4. Property validation
                var propertyValidation = await ValidatePropertiesAsync(xaml);
                results.AddRange(propertyValidation);

                // 5. Resource validation
                var resourceValidation = await ValidateResourcesAsync(xaml);
                results.AddRange(resourceValidation);

                if (!results.Any())
                {
                    return "XAML validation completed successfully. No issues found.";
                }

                return string.Join(Environment.NewLine, results);
            }
            catch (Exception ex)
            {
                return $"XAML validation error: {ex.Message}";
            }
        }

        /// <summary>
        /// Validates basic XML structure
        /// </summary>
        private async Task<List<string>> ValidateXmlStructureAsync(string xaml)
        {
            var results = new List<string>();

            try
            {
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.None,
                    ValidationFlags = XmlSchemaValidationFlags.None
                };

                using var reader = new StringReader(xaml);
                using var xmlReader = XmlReader.Create(reader, settings);

                while (xmlReader.Read())
                {
                    // Reading through the XML will catch basic structure errors
                }

                results.Add("=== XML Structure ===");
                results.Add("  XML structure is valid");
            }
            catch (XmlException ex)
            {
                results.Add("=== XML Structure Errors ===");
                results.Add($"  XML Error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Validates XAML-specific structure and syntax
        /// </summary>
        private async Task<List<string>> ValidateXamlStructureAsync(string xaml)
        {
            var results = new List<string>();

            try
            {
                // Parse as XML and validate XAML-specific patterns
                var doc = XDocument.Parse(xaml);
                var root = doc.Root;

                results.Add("=== XAML Structure ===");
                results.Add("  XAML can be parsed as valid XML");
                
                if (root != null)
                {
                    results.Add($"  Root element: {root.Name.LocalName}");
                    
                    // Check for common XAML root elements
                    var commonRoots = new[] { "Window", "UserControl", "Page", "Application", "ResourceDictionary", "Grid", "StackPanel" };
                    if (commonRoots.Contains(root.Name.LocalName))
                    {
                        results.Add($"  Recognized XAML root element type");
                    }
                    
                    // Check for x:Class attribute (code-behind)
                    var classAttribute = root.Attributes()
                        .FirstOrDefault(a => a.Name.LocalName == "Class" && 
                                           a.Name.NamespaceName == "http://schemas.microsoft.com/winfx/2006/xaml");
                    if (classAttribute != null)
                    {
                        results.Add($"  Code-behind class: {classAttribute.Value}");
                    }
                }
            }
            catch (XmlException ex)
            {
                results.Add("=== XAML Structure Errors ===");
                results.Add($"  XML Parse Error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}");
            }
            catch (Exception ex)
            {
                results.Add("=== XAML Structure Errors ===");
                results.Add($"  XAML Error: {ex.Message}");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Validates XAML namespaces
        /// </summary>
        private async Task<List<string>> ValidateNamespacesAsync(string xaml)
        {
            var results = new List<string>();

            try
            {
                var doc = XDocument.Parse(xaml);
                var root = doc.Root;

                if (root != null)
                {
                    results.Add("=== Namespace Analysis ===");
                    
                    var namespaces = root.Attributes()
                        .Where(a => a.IsNamespaceDeclaration)
                        .ToList();

                    results.Add($"  Declared namespaces: {namespaces.Count}");

                    foreach (var ns in namespaces)
                    {
                        var prefix = ns.Name.LocalName == "xmlns" ? "(default)" : ns.Name.LocalName;
                        results.Add($"    {prefix}: {ns.Value}");
                    }

                    // Check for common WPF/UWP namespaces
                    var commonNamespaces = new Dictionary<string, string>
                    {
                        ["http://schemas.microsoft.com/winfx/2006/xaml/presentation"] = "WPF Presentation",
                        ["http://schemas.microsoft.com/winfx/2006/xaml"] = "XAML",
                        ["http://schemas.microsoft.com/winfx/2006/xaml/composite-font"] = "Composite Font",
                        ["http://schemas.microsoft.com/expression/blend/2008"] = "Expression Blend"
                    };

                    var foundCommonNamespaces = namespaces
                        .Where(ns => commonNamespaces.ContainsKey(ns.Value))
                        .ToList();

                    if (foundCommonNamespaces.Any())
                    {
                        results.Add("  Recognized frameworks:");
                        foreach (var ns in foundCommonNamespaces)
                        {
                            results.Add($"    {commonNamespaces[ns.Value]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add("=== Namespace Validation Errors ===");
                results.Add($"  Error analyzing namespaces: {ex.Message}");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Validates XAML properties and attributes
        /// </summary>
        private async Task<List<string>> ValidatePropertiesAsync(string xaml)
        {
            var results = new List<string>();

            try
            {
                var doc = XDocument.Parse(xaml);
                var elements = doc.Descendants().ToList();

                results.Add("=== Property Analysis ===");
                results.Add($"  Total elements: {elements.Count}");

                var elementsWithProperties = elements.Where(e => e.Attributes().Any()).Count();
                results.Add($"  Elements with properties: {elementsWithProperties}");

                // Check for common property issues
                var issues = new List<string>();

                foreach (var element in elements)
                {
                    // Check for duplicate properties
                    var attributes = element.Attributes().Where(a => !a.IsNamespaceDeclaration).ToList();
                    var duplicates = attributes.GroupBy(a => a.Name.LocalName)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key);

                    foreach (var duplicate in duplicates)
                    {
                        issues.Add($"    Duplicate property '{duplicate}' on element '{element.Name.LocalName}'");
                    }

                    // Check for empty property values
                    var emptyProperties = attributes.Where(a => string.IsNullOrWhiteSpace(a.Value)).ToList();
                    foreach (var empty in emptyProperties)
                    {
                        issues.Add($"    Empty property '{empty.Name.LocalName}' on element '{element.Name.LocalName}'");
                    }
                }

                if (issues.Any())
                {
                    results.Add("  Property issues found:");
                    results.AddRange(issues);
                }
                else
                {
                    results.Add("  No property issues found");
                }
            }
            catch (Exception ex)
            {
                results.Add("=== Property Validation Errors ===");
                results.Add($"  Error analyzing properties: {ex.Message}");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Validates XAML resources
        /// </summary>
        private async Task<List<string>> ValidateResourcesAsync(string xaml)
        {
            var results = new List<string>();

            try
            {
                var doc = XDocument.Parse(xaml);
                
                // Find resource dictionaries
                var resourceElements = doc.Descendants()
                    .Where(e => e.Name.LocalName.Contains("Resources") || 
                               e.Name.LocalName == "ResourceDictionary")
                    .ToList();

                results.Add("=== Resource Analysis ===");
                results.Add($"  Resource containers found: {resourceElements.Count}");

                foreach (var resourceElement in resourceElements)
                {
                    var resources = resourceElement.Elements().ToList();
                    results.Add($"  Resources in {resourceElement.Name.LocalName}: {resources.Count}");

                    // Check for resource keys
                    var resourcesWithKeys = resources.Where(r => 
                        r.Attribute("Key") != null || 
                        r.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) != null)
                        .Count();

                    if (resources.Count > 0)
                    {
                        results.Add($"    Resources with keys: {resourcesWithKeys}");
                        results.Add($"    Resources without keys: {resources.Count - resourcesWithKeys}");
                    }
                }

                if (!resourceElements.Any())
                {
                    results.Add("  No resource containers found");
                }
            }
            catch (Exception ex)
            {
                results.Add("=== Resource Validation Errors ===");
                results.Add($"  Error analyzing resources: {ex.Message}");
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Formats and beautifies XAML content
        /// </summary>
        public async Task<string> FormatXamlAsync(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return xaml;

            try
            {
                var doc = XDocument.Parse(xaml);
                
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = Environment.NewLine,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = true
                };

                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, settings);
                
                doc.Save(xmlWriter);
                return await Task.FromResult(stringWriter.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to format XAML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shuts down the XAML validator asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            _schemaSet = null;
            _validationErrors.Clear();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the XAML validator and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the XAML validator and releases resources
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose or the finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _schemaSet = null;
                    _validationErrors?.Clear();
                }

                _disposed = true;
            }
        }
    }
}