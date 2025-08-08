using System;
using System.IO;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;

namespace A3sist.Orchastrator.Agents
{
    public class FileEditorAgent
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<FileEditorAgent> _logger;

        public FileEditorAgent(IFileSystem fileSystem, ILogger<FileEditorAgent> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<AgentResult> HandleAsync(AgentInput input)
        {
            try
            {
                // Check if file exists
                if (await _fileSystem.FileExistsAsync(input.FilePath))
                {
                    // Create backup
                    var backupPath = $"{input.FilePath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                    var originalContent = await _fileSystem.ReadAllTextAsync(input.FilePath);

                    // Compare content to detect conflicts
                    if (originalContent == input.Content)
                    {
                        return AgentResult.CreateSuccess("No changes needed - content is identical", input.FilePath);
                    }

                    // Create backup before overwriting
                    await _fileSystem.WriteAllTextAsync(backupPath, originalContent);
                    _logger.LogInformation($"Created backup at {backupPath}");

                    var result = new AgentResult
                    {
                        FilePath = input.FilePath,
                        OriginalContent = originalContent,
                        NewContent = input.Content,
                        RequiresReview = true
                    };

                    // Write the new content
                    await _fileSystem.WriteAllTextAsync(input.FilePath, input.Content);

                    result.Success = true;
                    result.Message = "File updated successfully with backup created";
                    return result;
                }
                else
                {
                    // Create new file
                    await _fileSystem.WriteAllTextAsync(input.FilePath, input.Content);
                    return AgentResult.CreateSuccess("New file created successfully", input.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file {input.FilePath}");
                return AgentResult.CreateFailure($"Error processing file: {ex.Message}", ex, input.FilePath);
            }
        }
    }
}