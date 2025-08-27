using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using A3sist.Models;
using System.Net.Http;

namespace A3sist.Services
{
    public class RAGEngineService : IRAGEngineService
    {
        private readonly IA3sistConfigurationService _configService;
        private readonly HttpClient _httpClient;
        private LocalRAGConfig _localConfig;
        private RemoteRAGConfig _remoteConfig;
        private readonly List<DocumentInfo> _indexedDocuments;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _indexingCancellationToken;
        private IndexingStatus _currentIndexingStatus;

        public event EventHandler<IndexingProgressEventArgs> IndexingProgress;

        public RAGEngineService(IA3sistConfigurationService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _indexedDocuments = new List<DocumentInfo>();
            _currentIndexingStatus = new IndexingStatus { IsIndexing = false };

            InitializeDefaultConfigurations();
        }

        public async Task<bool> IndexWorkspaceAsync(string workspacePath)
        {
            if (_currentIndexingStatus.IsIndexing)
            {
                return false; // Already indexing
            }

            _indexingCancellationToken = new CancellationTokenSource();
            _currentIndexingStatus = new IndexingStatus
            {
                IsIndexing = true,
                Progress = 0,
                CurrentFile = "",
                FilesProcessed = 0,
                TotalFiles = 0,
                EstimatedTimeRemaining = TimeSpan.Zero
            };

            try
            {
                var files = await GetIndexableFilesAsync(workspacePath);
                _currentIndexingStatus.TotalFiles = files.Count;

                var startTime = DateTime.UtcNow;
                var documentsToIndex = new List<DocumentInfo>();

                for (int i = 0; i < files.Count; i++)
                {
                    if (_indexingCancellationToken.Token.IsCancellationRequested)
                        break;

                    var file = files[i];
                    _currentIndexingStatus.CurrentFile = Path.GetFileName(file);
                    _currentIndexingStatus.FilesProcessed = i;
                    _currentIndexingStatus.Progress = (double)i / files.Count * 100;

                    // Estimate remaining time
                    if (i > 0)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var avgTimePerFile = elapsed.TotalMilliseconds / i;
                        var remainingFiles = files.Count - i;
                        _currentIndexingStatus.EstimatedTimeRemaining = TimeSpan.FromMilliseconds(avgTimePerFile * remainingFiles);
                    }

                    IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
                    {
                        Status = _currentIndexingStatus
                    });

                    try
                    {
                        var content = File.ReadAllText(file);
                        var document = new DocumentInfo
                        {
                            Id = GenerateDocumentId(file),
                            Path = file,
                            Content = content,
                            Language = DetectLanguage(file),
                            LastModified = File.GetLastWriteTime(file),
                            Size = content.Length
                        };

                        documentsToIndex.Add(document);

                        // Index in batches of 10 files
                        if (documentsToIndex.Count >= 10 || i == files.Count - 1)
                        {
                            await IndexDocumentsBatchAsync(documentsToIndex);
                            documentsToIndex.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue with next file
                        continue;
                    }
                }

                _currentIndexingStatus.IsIndexing = false;
                _currentIndexingStatus.Progress = 100;
                _currentIndexingStatus.CurrentFile = "Indexing completed";

                IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
                {
                    Status = _currentIndexingStatus
                });

                return true;
            }
            catch (Exception ex)
            {
                _currentIndexingStatus.IsIndexing = false;
                _currentIndexingStatus.CurrentFile = $"Error: {ex.Message}";
                
                IndexingProgress?.Invoke(this, new IndexingProgressEventArgs
                {
                    Status = _currentIndexingStatus
                });

                return false;
            }
        }

        private async Task<List<string>> GetIndexableFilesAsync(string workspacePath)
        {
            var supportedExtensions = new[]
            {
                ".cs", ".csx", ".vb", ".fs", ".fsx", // .NET languages
                ".js", ".ts", ".jsx", ".tsx", // JavaScript/TypeScript
                ".py", ".pyx", ".pyi", // Python
                ".java", ".kt", ".scala", // JVM languages
                ".cpp", ".c", ".cc", ".cxx", ".h", ".hpp", // C/C++
                ".rs", ".go", ".rb", ".php", ".swift", // Other languages
                ".sql", ".json", ".xml", ".yaml", ".yml", // Data formats
                ".md", ".txt", ".rst", ".adoc", // Documentation
                ".html", ".css", ".scss", ".sass", ".less" // Web
            };

            var files = new List<string>();

            foreach (var extension in supportedExtensions)
            {
                try
                {
                    var pattern = $"*{extension}";
                    var foundFiles = Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories);
                    files.AddRange(foundFiles);
                }
                catch
                {
                    // Continue with next extension
                }
            }

            // Filter out common excluded directories
            var excludedPaths = new[] { "bin", "obj", "node_modules", ".git", ".vs", "packages", "target", "build", "dist" };
            return files.Where(f => !excludedPaths.Any(e => f.Contains($"{Path.DirectorySeparatorChar}{e}{Path.DirectorySeparatorChar}"))).ToList();
        }

        private async Task IndexDocumentsBatchAsync(List<DocumentInfo> documents)
        {
            // Index to local vector store if configured
            if (_localConfig != null)
            {
                await IndexToLocalAsync(documents);
            }

            // Index to remote vector store if configured
            if (_remoteConfig != null)
            {
                await IndexToRemoteAsync(documents);
            }

            // Add to local document tracking
            lock (_lockObject)
            {
                foreach (var doc in documents)
                {
                    var existing = _indexedDocuments.FirstOrDefault(d => d.Id == doc.Id);
                    if (existing != null)
                    {
                        var index = _indexedDocuments.IndexOf(existing);
                        _indexedDocuments[index] = doc;
                    }
                    else
                    {
                        _indexedDocuments.Add(doc);
                    }
                }
            }
        }

        private async Task IndexToLocalAsync(List<DocumentInfo> documents)
        {
            try
            {
                // For now, implement a simple text-based similarity search
                // In a real implementation, you would use a proper vector database like Chroma, FAISS, etc.
                
                var localIndexPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "A3sist", "LocalIndex"
                );
                Directory.CreateDirectory(localIndexPath);

                foreach (var document in documents)
                {
                    var indexFile = Path.Combine(localIndexPath, $"{document.Id}.json");
                    var indexData = new
                    {
                        document.Id,
                        document.Path,
                        document.Content,
                        document.Language,
                        document.LastModified,
                        document.Size,
                        Chunks = ChunkDocument(document.Content),
                        Keywords = ExtractKeywords(document.Content),
                        Embeddings = await GenerateLocalEmbeddingsAsync(document.Content)
                    };

                    var json = JsonSerializer.Serialize(indexData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(indexFile, json);
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private async Task IndexToRemoteAsync(List<DocumentInfo> documents)
        {
            try
            {
                if (string.IsNullOrEmpty(_remoteConfig?.Endpoint) || string.IsNullOrEmpty(_remoteConfig?.ApiKey))
                    return;

                foreach (var document in documents)
                {
                    var chunks = ChunkDocument(document.Content);
                    
                    foreach (var chunk in chunks)
                    {
                        var indexRequest = new
                        {
                            id = $"{document.Id}_{chunks.IndexOf(chunk)}",
                            vector = await GenerateRemoteEmbeddingsAsync(chunk),
                            metadata = new
                            {
                                document_id = document.Id,
                                document_path = document.Path,
                                language = document.Language,
                                chunk_index = chunks.IndexOf(chunk),
                                content = chunk
                            }
                        };

                        await SendRemoteIndexRequestAsync(indexRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private async Task<double[]> GenerateLocalEmbeddingsAsync(string text)
        {
            // For now, generate simple hash-based embeddings
            // In a real implementation, you would use a local embedding model like sentence-transformers
            var hash = text.GetHashCode();
            var random = new Random(hash);
            var embeddings = new double[384]; // Common embedding dimension
            
            for (int i = 0; i < embeddings.Length; i++)
            {
                embeddings[i] = random.NextDouble() * 2 - 1; // Random values between -1 and 1
            }
            
            return embeddings;
        }

        private async Task<double[]> GenerateRemoteEmbeddingsAsync(string text)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_remoteConfig.Endpoint}/embeddings");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _remoteConfig.ApiKey);

                var requestBody = new
                {
                    input = text,
                    model = _remoteConfig.EmbeddingModel ?? "text-embedding-ada-002"
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var embeddings = result.GetProperty("data")[0].GetProperty("embedding");
                    return embeddings.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                }
            }
            catch
            {
                // Fallback to local embeddings
            }

            return await GenerateLocalEmbeddingsAsync(text);
        }

        private async Task SendRemoteIndexRequestAsync(object indexRequest)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_remoteConfig.Endpoint}/vectors/upsert");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _remoteConfig.ApiKey);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { vectors = new[] { indexRequest } }),
                    Encoding.UTF8,
                    "application/json"
                );

                await _httpClient.SendAsync(request);
            }
            catch
            {
                // Log error
            }
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 10)
        {
            var results = new List<SearchResult>();

            // Search local index
            if (_localConfig != null)
            {
                var localResults = await SearchLocalAsync(query, maxResults);
                results.AddRange(localResults);
            }

            // Search remote index
            if (_remoteConfig != null)
            {
                var remoteResults = await SearchRemoteAsync(query, maxResults);
                results.AddRange(remoteResults);
            }

            // Merge and rank results
            return results.OrderByDescending(r => r.Score).Take(maxResults);
        }

        private async Task<IEnumerable<SearchResult>> SearchLocalAsync(string query, int maxResults)
        {
            var results = new List<SearchResult>();

            try
            {
                var localIndexPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "A3sist", "LocalIndex"
                );

                if (!Directory.Exists(localIndexPath))
                    return results;

                var indexFiles = Directory.GetFiles(localIndexPath, "*.json");
                var queryEmbeddings = await GenerateLocalEmbeddingsAsync(query);

                foreach (var indexFile in indexFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(indexFile);
                        var indexData = JsonSerializer.Deserialize<JsonElement>(content);
                        
                        var documentContent = indexData.GetProperty("Content").GetString();
                        var documentPath = indexData.GetProperty("Path").GetString();
                        var documentId = indexData.GetProperty("Id").GetString();

                        // Simple text-based similarity for now
                        var similarity = CalculateTextSimilarity(query, documentContent);

                        if (similarity > (_localConfig?.SimilarityThreshold ?? 0.5))
                        {
                            results.Add(new SearchResult
                            {
                                DocumentId = documentId,
                                Content = TruncateContent(documentContent, query),
                                Score = similarity,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["path"] = documentPath,
                                    ["source"] = "local"
                                }
                            });
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Log error
            }

            return results.OrderByDescending(r => r.Score).Take(maxResults);
        }

        private async Task<IEnumerable<SearchResult>> SearchRemoteAsync(string query, int maxResults)
        {
            var results = new List<SearchResult>();

            try
            {
                if (string.IsNullOrEmpty(_remoteConfig?.Endpoint) || string.IsNullOrEmpty(_remoteConfig?.ApiKey))
                    return results;

                var queryEmbeddings = await GenerateRemoteEmbeddingsAsync(query);
                
                var searchRequest = new HttpRequestMessage(HttpMethod.Post, $"{_remoteConfig.Endpoint}/query");
                searchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _remoteConfig.ApiKey);

                var requestBody = new
                {
                    vector = queryEmbeddings,
                    top_k = maxResults,
                    include_metadata = true
                };

                searchRequest.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(searchRequest);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (result.TryGetProperty("matches", out var matches))
                    {
                        foreach (var match in matches.EnumerateArray())
                        {
                            var score = match.GetProperty("score").GetDouble();
                            var metadata = match.GetProperty("metadata");
                            var content = metadata.GetProperty("content").GetString();
                            var documentId = metadata.GetProperty("document_id").GetString();
                            var documentPath = metadata.GetProperty("document_path").GetString();

                            results.Add(new SearchResult
                            {
                                DocumentId = documentId,
                                Content = content,
                                Score = score,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["path"] = documentPath,
                                    ["source"] = "remote"
                                }
                            });
                        }
                    }
                }
            }
            catch
            {
                // Log error
            }

            return results;
        }

        public async Task<bool> AddDocumentAsync(string documentPath, string content)
        {
            var document = new DocumentInfo
            {
                Id = GenerateDocumentId(documentPath),
                Path = documentPath,
                Content = content,
                Language = DetectLanguage(documentPath),
                LastModified = DateTime.UtcNow,
                Size = content.Length
            };

            await IndexDocumentsBatchAsync(new List<DocumentInfo> { document });
            return true;
        }

        public async Task<bool> RemoveDocumentAsync(string documentPath)
        {
            var documentId = GenerateDocumentId(documentPath);

            // Remove from local tracking
            lock (_lockObject)
            {
                var document = _indexedDocuments.FirstOrDefault(d => d.Id == documentId);
                if (document != null)
                {
                    _indexedDocuments.Remove(document);
                }
            }

            // Remove from local index
            if (_localConfig != null)
            {
                var localIndexPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "A3sist", "LocalIndex"
                );
                var indexFile = Path.Combine(localIndexPath, $"{documentId}.json");
                if (File.Exists(indexFile))
                {
                    File.Delete(indexFile);
                }
            }

            // Remove from remote index
            if (_remoteConfig != null)
            {
                try
                {
                    var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{_remoteConfig.Endpoint}/vectors/{documentId}");
                    deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _remoteConfig.ApiKey);
                    await _httpClient.SendAsync(deleteRequest);
                }
                catch
                {
                    // Log error
                }
            }

            return true;
        }

        public async Task<IndexingStatus> GetIndexingStatusAsync()
        {
            return _currentIndexingStatus;
        }

        public async Task<bool> ConfigureLocalRAGAsync(LocalRAGConfig config)
        {
            _localConfig = config;
            await _configService.SetSettingAsync("rag.localConfig", config);
            return true;
        }

        public async Task<bool> ConfigureRemoteRAGAsync(RemoteRAGConfig config)
        {
            _remoteConfig = config;
            await _configService.SetSettingAsync("rag.remoteConfig", config);
            return true;
        }

        private void InitializeDefaultConfigurations()
        {
            _localConfig = new LocalRAGConfig
            {
                VectorStoreType = "SimpleText",
                DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "A3sist", "LocalIndex"),
                EmbeddingModel = "local-simple",
                EmbeddingDimensions = 384,
                SimilarityThreshold = 0.7
            };
        }

        private string GenerateDocumentId(string path)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(path)).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string DetectLanguage(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    return "csharp";
                case ".vb":
                    return "vbnet";
                case ".fs":
                    return "fsharp";
                case ".js":
                    return "javascript";
                case ".ts":
                    return "typescript";
                case ".py":
                    return "python";
                case ".java":
                    return "java";
                case ".cpp":
                case ".c":
                case ".cc":
                case ".cxx":
                    return "cpp";
                case ".h":
                case ".hpp":
                    return "cpp";
                case ".rs":
                    return "rust";
                case ".go":
                    return "go";
                case ".rb":
                    return "ruby";
                case ".php":
                    return "php";
                case ".swift":
                    return "swift";
                case ".kt":
                    return "kotlin";
                case ".scala":
                    return "scala";
                case ".sql":
                    return "sql";
                case ".json":
                    return "json";
                case ".xml":
                    return "xml";
                case ".yaml":
                case ".yml":
                    return "yaml";
                case ".md":
                    return "markdown";
                case ".html":
                    return "html";
                case ".css":
                    return "css";
                case ".scss":
                case ".sass":
                    return "scss";
                default:
                    return "text";
            }
        }

        private List<string> ChunkDocument(string content, int maxChunkSize = 1000)
        {
            var chunks = new List<string>();
            var lines = content.Split('\n');
            var currentChunk = new StringBuilder();

            foreach (var line in lines)
            {
                if (currentChunk.Length + line.Length > maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }
                currentChunk.AppendLine(line);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
            }

            return chunks;
        }

        private List<string> ExtractKeywords(string content)
        {
            // Simple keyword extraction
            var words = content.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Where(w => w.Length > 3).Distinct().Take(20).ToList();
        }

        private double CalculateTextSimilarity(string query, string document)
        {
            // Simple Jaccard similarity
            var queryWords = query.ToLowerInvariant().Split(' ').ToHashSet();
            var documentWords = document.ToLowerInvariant().Split(' ').ToHashSet();
            
            var intersection = queryWords.Intersect(documentWords).Count();
            var union = queryWords.Union(documentWords).Count();
            
            return union > 0 ? (double)intersection / union : 0;
        }

        private string TruncateContent(string content, string query, int maxLength = 500)
        {
            if (content.Length <= maxLength)
                return content;

            // Try to find the query in the content and show context around it
            var queryIndex = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (queryIndex >= 0)
            {
                var start = Math.Max(0, queryIndex - maxLength / 2);
                var length = Math.Min(maxLength, content.Length - start);
                return content.Substring(start, length);
            }

            return content.Substring(0, maxLength);
        }

        public void Dispose()
        {
            _indexingCancellationToken?.Cancel();
            _httpClient?.Dispose();
        }

        private class DocumentInfo
        {
            public string Id { get; set; }
            public string Path { get; set; }
            public string Content { get; set; }
            public string Language { get; set; }
            public DateTime LastModified { get; set; }
            public long Size { get; set; }
        }
    }
}