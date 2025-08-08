// File: c:\Repo\A3sist\Orchastrator\LLM\LLMResponse.cs
public class LLMResponse
{
    public string Completion { get; set; }  // The generated text from the LLM
    public int TokensUsed { get; set; }     // Number of tokens used in the request
    public bool IsTruncated { get; set; }   // Whether the response was truncated
    public string Model { get; set; }      // The model used for generation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Timestamp of response
}