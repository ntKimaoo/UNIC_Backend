namespace BusinessLogic.Options
{
    public class OpenRouterOptions
    {
        public const string SectionName = "OpenRouterAI";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "qwen/qwen3.6-plus:free";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int TimeoutSeconds { get; set; } = 120;
    }
}
