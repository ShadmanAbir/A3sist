using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Utils;

namespace A3sist.Agents.IntentRouter.Services
{
    public class FailureAnalyzer
    {
        private List<FailureRecord> _failureRecords = new List<FailureRecord>();
        private readonly string _failureNotesPath;

        public FailureAnalyzer()
        {
            _failureNotesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectGoals", "failure-notes.md");
        }

        public async Task InitializeAsync()
        {
            // Load failure records from the failure notes file
            if (File.Exists(_failureNotesPath))
            {
                var content = File.ReadAllText(_failureNotesPath);
                _failureRecords = ParseFailureRecords(content);
            }
        }

        private List<FailureRecord> ParseFailureRecords(string content)
        {
            var records = new List<FailureRecord>();
            var sections = content.Split(new[] { "### Task:" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                try
                {
                    var lines = section.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var record = new FailureRecord();

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Task:"))
                        {
                            record.TaskDescription = line.Substring("Task:".Length).Trim();
                        }
                        else if (line.StartsWith("Failure Description:"))
                        {
                            record.FailureDescription = line.Substring("Failure Description:".Length).Trim();
                        }
                        else if (line.StartsWith("Why It Failed:"))
                        {
                            record.Reason = line.Substring("Why It Failed:".Length).Trim();
                        }
                        else if (line.StartsWith("Attempts to Fix:"))
                        {
                            record.Attempts = line.Substring("Attempts to Fix:".Length).Trim();
                        }
                        else if (line.StartsWith("Resolution / Next Steps:"))
                        {
                            record.Resolution = line.Substring("Resolution / Next Steps:".Length).Trim();
                        }
                    }

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to parse failure record: {ex.Message}");
                }
            }

            return records;
        }

        public async Task<FailureAnalysis> AnalyzeFailureAsync(string taskDescription)
        {
            // Check if this task has failed before
            var matchingRecords = _failureRecords
                .Where(r => r.TaskDescription.Contains(taskDescription))
                .ToList();

            if (matchingRecords.Any())
            {
                // Find the most recent failure
                var mostRecent = matchingRecords.OrderByDescending(r => r.Timestamp).First();

                return new FailureAnalysis
                {
                    HasFailedBefore = true,
                    FailedAgent = mostRecent.FailedAgent,
                    FailureDescription = mostRecent.FailureDescription,
                    Reason = mostRecent.Reason,
                    Attempts = mostRecent.Attempts,
                    Resolution = mostRecent.Resolution,
                    Timestamp = mostRecent.Timestamp
                };
            }

            return new FailureAnalysis { HasFailedBefore = false };
        }

        public async Task LogFailureAsync(FailureRecord record)
        {
            // Add to in-memory records
            _failureRecords.Add(record);

            // Append to the failure notes file
            var entry = $@"
### Task:
{record.TaskDescription}

### Failure Description:
{record.FailureDescription}

### Why It Failed:
{record.Reason}

### Attempts to Fix:
{record.Attempts}

### Resolution / Next Steps:
{record.Resolution}
";

            File.AppendAllText(_failureNotesPath, entry);
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            _failureRecords.Clear();
        }
    }

    public class FailureRecord
    {
        public string TaskDescription { get; set; }
        public string FailureDescription { get; set; }
        public string Reason { get; set; }
        public string Attempts { get; set; }
        public string Resolution { get; set; }
        public string FailedAgent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class FailureAnalysis
    {
        public bool HasFailedBefore { get; set; }
        public string FailedAgent { get; set; }
        public string FailureDescription { get; set; }
        public string Reason { get; set; }
        public string Attempts { get; set; }
        public string Resolution { get; set; }
        public DateTime Timestamp { get; set; }
    }
}