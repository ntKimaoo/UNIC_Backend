namespace BusinessLogic.Options
{
    public class PayOSOptions
    {
        public const string SectionName = "PayOS";

        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string PublicBaseUrl { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public int LinkExpirationMinutes { get; set; } = 60;
        public bool UseMock { get; set; }
        public string MockCheckoutUrlTemplate { get; set; } = string.Empty;
    }
}
