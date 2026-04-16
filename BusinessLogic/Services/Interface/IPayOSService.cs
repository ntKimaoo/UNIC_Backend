namespace BusinessLogic.Services.Interface
{
    public class PayOSMerchantCredential
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
    }

    public interface IPayOSService
    {
        Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
            PayOSMerchantCredential merchant,
            long orderCode,
            decimal amountVnd,
            string description,
            CancellationToken cancellationToken = default);
        bool VerifyWebhookSignature(string checksumKey, string receivedSignature, System.Text.Json.JsonElement dataElement);
    }

    public class PayOSPaymentLinkResult
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
    }
}
