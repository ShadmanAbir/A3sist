using System;
using System.Threading.Tasks;

namespace CodeAssist.Agents.Analyzer
{
    public abstract class BaseAnalyzerAgent : BaseAgent
    {
        protected string Language { get; set; }
        protected string AnalysisType { get; set; }

        public BaseAnalyzerAgent(string name, string version, string language, string analysisType)
            : base(name, version)
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
            AnalysisType = analysisType ?? throw new ArgumentNullException(nameof(analysisType));
        }

        protected override async Task OnInitializeAsync()
        {
            Console.WriteLine($"Initializing {AgentName} v{AgentVersion} for {Language} {AnalysisType} analysis");

            // Load language-specific configuration
            await LoadLanguageConfiguration();

            // Initialize language-specific analyzers
            await InitializeLanguageAnalyzers();
        }

        protected override async Task OnExecuteAsync()
        {
            Console.WriteLine($"Executing {AgentName} for {Language} {AnalysisType} analysis");

            // Perform language-specific analysis
            await PerformAnalysis();

            // Process and report results
            await ProcessResults();
        }

        protected override async Task OnShutdownAsync()
        {
            Console.WriteLine($"Shutting down {AgentName}");

            // Clean up language-specific analyzers
            await CleanupAnalyzers();
        }

        protected abstract Task LoadLanguageConfiguration();
        protected abstract Task InitializeLanguageAnalyzers();
        protected abstract Task PerformAnalysis();
        protected abstract Task ProcessResults();
        protected abstract Task CleanupAnalyzers();
    }
}