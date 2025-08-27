namespace A3sist.Core.LLM
{
    public class LLMOptions
    {
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public string[]? Stop { get; set; }
    }
}