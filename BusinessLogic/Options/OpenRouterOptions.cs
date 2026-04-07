namespace BusinessLogic.Options
{
    public class OpenRouterOptions
    {
        public const string SectionName = "OpenRouterAI";

        public string ApiKey { get; set; } = "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";
        public string Model { get; set; } = "google/gemma-4-26b-a4b-it";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int TimeoutSeconds { get; set; } = 120;
    }
}
