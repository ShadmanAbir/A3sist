using System;

namespace A3sist.Shared.Messaging
{
    public class AgentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Exception Exception { get; set; }
        public string OriginalContent { get; set; }
        public string NewContent { get; set; }
        public bool RequiresReview { get; set; }

        public static AgentResult CreateSuccess(string message, string filePath = null)
        {
            return new AgentResult
            {
                Success = true,
                Message = message,
                FilePath = filePath
            };
        }

        public static AgentResult CreateFailure(string message, Exception ex = null, string filePath = null)
        {
            return new AgentResult
            {
                Success = false,
                Message = message,
                Exception = ex,
                FilePath = filePath
            };
        }
    }
}