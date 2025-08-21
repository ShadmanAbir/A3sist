using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for code analysis services used by refactoring agents
    /// </summary>
    public interface ICodeAnalysisService
    {
        /// <summary>
        /// Analyzes code structure and identifies patterns
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Code analysis result</returns>
        Task<CodeAnalysisResult> AnalyzeCodeAsync(string code, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects code smells and anti-patterns
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of detected code smells</returns>
        Task<IEnumerable<CodeSmell>> DetectCodeSmellsAsync(string code, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates code complexity metrics
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complexity metrics</returns>
        Task<ComplexityMetrics> CalculateComplexityAsync(string code, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifies dependencies and coupling issues
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <param name="filePath">The file path for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dependency analysis result</returns>
        Task<DependencyAnalysisResult> AnalyzeDependenciesAsync(string code, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of code analysis
    /// </summary>
    public class CodeAnalysisResult
    {
        public string Language { get; set; }
        public IEnumerable<CodeElement> Elements { get; set; } = new List<CodeElement>();
        public IEnumerable<CodePattern> Patterns { get; set; } = new List<CodePattern>();
        public ComplexityMetrics Complexity { get; set; }
        public IEnumerable<CodeSmell> CodeSmells { get; set; } = new List<CodeSmell>();
        public DependencyAnalysisResult Dependencies { get; set; }
    }

    /// <summary>
    /// Represents a code element (class, method, property, etc.)
    /// </summary>
    public class CodeElement
    {
        public string Name { get; set; }
        public CodeElementType Type { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string Signature { get; set; }
        public IEnumerable<string> Modifiers { get; set; } = new List<string>();
        public IEnumerable<CodeElement> Children { get; set; } = new List<CodeElement>();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of code elements
    /// </summary>
    public enum CodeElementType
    {
        Namespace,
        Class,
        Interface,
        Struct,
        Enum,
        Method,
        Property,
        Field,
        Event,
        Constructor,
        Destructor,
        Operator,
        Indexer,
        Parameter,
        LocalVariable,
        Lambda,
        AnonymousMethod
    }

    /// <summary>
    /// Represents a detected code pattern
    /// </summary>
    public class CodePattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PatternType Type { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Types of code patterns
    /// </summary>
    public enum PatternType
    {
        DesignPattern,
        AntiPattern,
        CodeSmell,
        BestPractice,
        PerformancePattern,
        SecurityPattern
    }

    /// <summary>
    /// Represents a code smell
    /// </summary>
    public class CodeSmell
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CodeSmellType Type { get; set; }
        public CodeSmellSeverity Severity { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string Suggestion { get; set; }
        public IEnumerable<RefactoringType> SuggestedRefactorings { get; set; } = new List<RefactoringType>();
    }

    /// <summary>
    /// Types of code smells
    /// </summary>
    public enum CodeSmellType
    {
        LongMethod,
        LargeClass,
        LongParameterList,
        DuplicatedCode,
        DeadCode,
        SpeculativeGenerality,
        FeatureEnvy,
        DataClumps,
        PrimitiveObsession,
        SwitchStatements,
        ParallelInheritanceHierarchies,
        LazyClass,
        DataClass,
        RefusedBequest,
        AlternativeClassesWithDifferentInterfaces,
        TemporaryField,
        MessageChains,
        MiddleMan,
        InappropriateIntimacy,
        IncompleteLibraryClass,
        Comments,
        MagicNumbers,
        ComplexConditional,
        LongIdentifier,
        ShortIdentifier,
        UncommunicativeNames,
        InconsistentNames,
        TypeEmbeddedInName,
        UnnecessaryComplexity,
        MissingAbstraction,
        ImproperInheritance,
        BrokenModularization,
        CyclomaticComplexity,
        ExcessiveClassLength,
        ExcessiveMethodLength,
        ExcessiveParameterList,
        ExcessivePublicCount,
        TooManyFields,
        TooManyMethods,
        LackOfCohesion,
        CouplingBetweenObjects,
        ResponseForClass,
        WeightedMethodsPerClass,
        DepthOfInheritanceTree,
        NumberOfChildren
    }

    /// <summary>
    /// Severity levels for code smells
    /// </summary>
    public enum CodeSmellSeverity
    {
        Info,
        Minor,
        Major,
        Critical,
        Blocker
    }

    /// <summary>
    /// Code complexity metrics
    /// </summary>
    public class ComplexityMetrics
    {
        public int CyclomaticComplexity { get; set; }
        public int LinesOfCode { get; set; }
        public int EffectiveLinesOfCode { get; set; }
        public int NumberOfMethods { get; set; }
        public int NumberOfClasses { get; set; }
        public int NumberOfInterfaces { get; set; }
        public int DepthOfInheritance { get; set; }
        public int NumberOfChildren { get; set; }
        public double CouplingBetweenObjects { get; set; }
        public double LackOfCohesion { get; set; }
        public int WeightedMethodsPerClass { get; set; }
        public int ResponseForClass { get; set; }
        public double MaintainabilityIndex { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Result of dependency analysis
    /// </summary>
    public class DependencyAnalysisResult
    {
        public IEnumerable<Dependency> Dependencies { get; set; } = new List<Dependency>();
        public IEnumerable<string> ExternalDependencies { get; set; } = new List<string>();
        public IEnumerable<string> UnusedDependencies { get; set; } = new List<string>();
        public IEnumerable<CircularDependency> CircularDependencies { get; set; } = new List<CircularDependency>();
        public double CouplingMetric { get; set; }
        public double CohesionMetric { get; set; }
    }

    /// <summary>
    /// Represents a dependency between code elements
    /// </summary>
    public class Dependency
    {
        public string From { get; set; }
        public string To { get; set; }
        public DependencyType Type { get; set; }
        public int Strength { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Types of dependencies
    /// </summary>
    public enum DependencyType
    {
        Inheritance,
        Composition,
        Aggregation,
        Association,
        Usage,
        Import,
        Call,
        Reference
    }

    /// <summary>
    /// Represents a circular dependency
    /// </summary>
    public class CircularDependency
    {
        public IEnumerable<string> Cycle { get; set; } = new List<string>();
        public string Description { get; set; }
        public CircularDependencySeverity Severity { get; set; }
    }

    /// <summary>
    /// Severity levels for circular dependencies
    /// </summary>
    public enum CircularDependencySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}