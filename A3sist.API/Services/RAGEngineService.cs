using A3sist.API.Models;
using A3sist.API.Services;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace A3sist.API.Services;

public class RAGEngineService : IRAGEngineService, IDisposable
{
    private readonly ILogger<RAGEngineService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, SearchResult> _indexedDocuments;
    private readonly SemaphoreSlim _indexingSemaphore;
    private volatile bool _isIndexing;
    private IndexingStatus _indexingStatus;
    private LocalRAGConfig? _localConfig;
    private RemoteRAGConfig? _remoteConfig;
    private bool _disposed;

    public event EventHandler<IndexingProgressEventArgs>? IndexingProgress;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".vb", ".fs", ".cpp", ".c", ".h", ".hpp", ".java", ".py", ".js", ".ts", 
        ".html", ".xml", ".json", ".yml", ".yaml", ".md", ".txt", ".sql", ".ps1", 
        ".xaml", ".razor", ".cshtml", ".config", ".settings", ".resx"
    };

    public RAGEngineService(ILogger<RAGEngineService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _indexedDocuments = new ConcurrentDictionary<string, SearchResult>();
        _indexingSemaphore = new SemaphoreSlim(1, 1);
        _indexingStatus = new IndexingStatus();
        
        // Initialize with default local configuration
        _localConfig = new LocalRAGConfig
        {
            IndexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "A3sist", "RAG", "index"),
            EmbeddingModel = "all-MiniLM-L6-v2",
            ChunkSize = 512,
            ChunkOverlap = 50,
            SupportedExtensions = SupportedExtensions.ToList(),
            VectorStoreType = "Simple",
            DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "A3sist", "RAG", "database.json"),
            EmbeddingDimensions = 384,
            SimilarityThreshold = 0.6
        };
    }

    public async Task<bool> IndexWorkspaceAsync(string workspacePath)
    {
        if (string.IsNullOrEmpty(workspacePath) || !Directory.Exists(workspacePath))
        {
            _logger.LogWarning("Workspace path is invalid: {WorkspacePath}", workspacePath);
            return false;
        }

        if (_isIndexing)
        {
            _logger.LogWarning("Indexing is already in progress");
            return false;
        }

        await _indexingSemaphore.WaitAsync();
        try
        {
            _isIndexing = true;
            _indexingStatus = new IndexingStatus
            {
                IsIndexing = true,
                TotalDocuments = 0,
                IndexedDocuments = 0,
                CurrentDocument = "",
                ProgressPercentage = 0,
                FilesProcessed = 0,
                TotalFiles = 0,
                Progress = 0,
                CurrentFile = "",
                EstimatedTimeRemaining = TimeSpan.Zero
            };

            _logger.LogInformation("Starting workspace indexing for: {WorkspacePath}", workspacePath);

            // Get all supported files
            var files = GetSupportedFiles(workspacePath);
            _indexingStatus.TotalFiles = files.Count;
            _indexingStatus.TotalDocuments = files.Count;

            var startTime = DateTime.UtcNow;
            var processedFiles = 0;

            foreach (var file in files)
            {
                try
                {
                    _indexingStatus.CurrentFile = file;
                    _indexingStatus.CurrentDocument = Path.GetFileName(file);
                    
                    await IndexDocumentAsync(file);
                    
                    processedFiles++;
                    _indexingStatus.FilesProcessed = processedFiles;
                    _indexingStatus.IndexedDocuments = processedFiles;
                    _indexingStatus.Progress = (double)processedFiles / files.Count * 100;
                    _indexingStatus.ProgressPercentage = _indexingStatus.Progress;

                    // Estimate remaining time
                    var elapsed = DateTime.UtcNow - startTime;
                    var remainingFiles = files.Count - processedFiles;
                    if (processedFiles > 0)
                    {
                        var avgTimePerFile = elapsed.TotalMilliseconds / processedFiles;
                        _indexingStatus.EstimatedTimeRemaining = TimeSpan.FromMilliseconds(avgTimePerFile * remainingFiles);
                    }

                    // Report progress
                    IndexingProgress?.Invoke(this, new IndexingProgressEventArgs { Status = _indexingStatus });

                    // Small delay to prevent overwhelming the system
                    await Task.Delay(10);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing file: {FilePath}", file);
                }
            }

            _indexingStatus.IsIndexing = false;
            _indexingStatus.Progress = 100;
            _indexingStatus.ProgressPercentage = 100;
            _indexingStatus.CurrentFile = "";
            _indexingStatus.CurrentDocument = "Completed";

            // Save index to disk
            await SaveIndexToDiskAsync();

            _logger.LogInformation("Workspace indexing completed. Indexed {Count} files in {Duration}", 
                processedFiles, DateTime.UtcNow - startTime);

            // Final progress report
            IndexingProgress?.Invoke(this, new IndexingProgressEventArgs { Status = _indexingStatus });

            return true;
        }
        finally
        {
            _isIndexing = false;
            _indexingSemaphore.Release();
        }
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 10)
    {
        if (string.IsNullOrEmpty(query))
            return Enumerable.Empty<SearchResult>();

        try
        {
            _logger.LogDebug("Searching for: {Query}", query);

            // Simple text-based search for now (can be enhanced with embeddings)
            var results = new List<SearchResult>();
            var queryLower = query.ToLowerInvariant();
            var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var doc in _indexedDocuments.Values)
            {
                var score = CalculateRelevanceScore(doc.Content, queryWords);
                if (score > 0.1) // Minimum relevance threshold
                {
                    results.Add(new SearchResult
                    {
                        Id = doc.Id,
                        Content = doc.Content,
                        FilePath = doc.FilePath,
                        Score = score,
                        Metadata = doc.Metadata,
                        DocumentId = doc.DocumentId
                    });
                }
            }

            // Sort by relevance score and take top results
            var topResults = results
                .OrderByDescending(r => r.Score)
                .Take(maxResults)
                .ToList();

            _logger.LogDebug("Found {Count} results for query: {Query}", topResults.Count, query);
            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for query: {Query}", query);
            return Enumerable.Empty<SearchResult>();
        }
    }

    public async Task<bool> AddDocumentAsync(string documentPath, string content)
    {
        if (string.IsNullOrEmpty(documentPath) || string.IsNullOrEmpty(content))
            return false;

        try
        {
            var documentId = GenerateDocumentId(documentPath);
            var chunks = ChunkContent(content, _localConfig?.ChunkSize ?? 512, _localConfig?.ChunkOverlap ?? 50);

            var chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                var searchResult = new SearchResult
                {
                    Id = $"{documentId}_chunk_{chunkIndex}",
                    Content = chunk,
                    FilePath = documentPath,
                    Score = 1.0,
                    DocumentId = documentId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["chunkIndex"] = chunkIndex,
                        ["totalChunks"] = chunks.Count,
                        ["fileExtension"] = Path.GetExtension(documentPath),
                        ["fileName"] = Path.GetFileName(documentPath),
                        ["indexedAt"] = DateTime.UtcNow
                    }
                };

                _indexedDocuments.AddOrUpdate(searchResult.Id, searchResult, (key, existing) => searchResult);
                chunkIndex++;
            }

            _logger.LogDebug("Added document with {ChunkCount} chunks: {DocumentPath}", chunks.Count, documentPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document: {DocumentPath}", documentPath);
            return false;
        }
    }

    public async Task<bool> RemoveDocumentAsync(string documentPath)
    {
        if (string.IsNullOrEmpty(documentPath))
            return false;

        try
        {
            var documentId = GenerateDocumentId(documentPath);
            var keysToRemove = _indexedDocuments.Keys.Where(k => k.StartsWith(documentId)).ToList();

            foreach (var key in keysToRemove)
            {
                _indexedDocuments.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed document: {DocumentPath} ({ChunkCount} chunks)", documentPath, keysToRemove.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document: {DocumentPath}", documentPath);
            return false;
        }
    }

    public async Task<IndexingStatus> GetIndexingStatusAsync()
    {
        return _indexingStatus;
    }

    public async Task<bool> ConfigureLocalRAGAsync(LocalRAGConfig config)
    {
        if (config == null)
            return false;

        try
        {
            _localConfig = config;
            
            // Ensure directories exist
            if (!string.IsNullOrEmpty(config.IndexPath))
            {
                Directory.CreateDirectory(config.IndexPath);
            }

            var dbPath = Path.GetDirectoryName(config.DatabasePath);
            if (!string.IsNullOrEmpty(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }

            _logger.LogInformation("Local RAG configuration updated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring local RAG");
            return false;
        }
    }

    public async Task<bool> ConfigureRemoteRAGAsync(RemoteRAGConfig config)
    {
        if (config == null)
            return false;

        try
        {
            _remoteConfig = config;
            _logger.LogInformation("Remote RAG configuration updated for endpoint: {Endpoint}", config.ApiEndpoint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring remote RAG");
            return false;
        }
    }

    private async Task<bool> IndexDocumentAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return await AddDocumentAsync(filePath, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file for indexing: {FilePath}", filePath);
            return false;
        }
    }

    private List<string> GetSupportedFiles(string directoryPath)
    {
        var files = new List<string>();
        
        try
        {
            var allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file);
                if (SupportedExtensions.Contains(extension))
                {
                    // Skip large files (> 1MB for now)
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length <= 1024 * 1024) // 1MB limit
                    {
                        files.Add(file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files from directory: {DirectoryPath}", directoryPath);
        }

        return files;
    }

    private List<string> ChunkContent(string content, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        
        if (content.Length <= chunkSize)
        {
            chunks.Add(content);
            return chunks;
        }

        for (int i = 0; i < content.Length; i += chunkSize - overlap)
        {
            var end = Math.Min(i + chunkSize, content.Length);
            var chunk = content.Substring(i, end - i);
            
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (end >= content.Length)
                break;
        }

        return chunks;
    }

    private double CalculateRelevanceScore(string content, string[] queryWords)
    {
        if (string.IsNullOrEmpty(content) || queryWords.Length == 0)
            return 0;

        var contentLower = content.ToLowerInvariant();
        var score = 0.0;
        var totalWords = queryWords.Length;

        foreach (var word in queryWords)
        {
            if (string.IsNullOrEmpty(word))
                continue;

            // Exact word match gets full score
            var exactMatches = Regex.Matches(contentLower, $@"\b{Regex.Escape(word)}\b").Count;
            score += exactMatches * 1.0;

            // Partial matches get partial score
            var partialMatches = Regex.Matches(contentLower, Regex.Escape(word)).Count - exactMatches;
            score += partialMatches * 0.5;
        }

        // Normalize score by content length and query terms
        var normalizedScore = score / (totalWords * Math.Max(1, Math.Log(content.Length / 100.0)));
        return Math.Min(normalizedScore, 1.0);
    }

    private string GenerateDocumentId(string documentPath)
    {
        // Use relative path as document ID to handle path changes
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(documentPath))
               .Replace('+', '-').Replace('/', '_').Replace('=', '');
    }

    private async Task SaveIndexToDiskAsync()
    {
        if (_localConfig?.DatabasePath == null)
            return;

        try
        {
            var indexData = new
            {
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                DocumentCount = _indexedDocuments.Count,
                Documents = _indexedDocuments.Values.ToList()
            };

            var json = JsonSerializer.Serialize(indexData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_localConfig.DatabasePath, json);
            
            _logger.LogDebug("Index saved to disk: {DatabasePath}", _localConfig.DatabasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving index to disk");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _indexingSemaphore.Dispose();
            _disposed = true;
        }
    }
}