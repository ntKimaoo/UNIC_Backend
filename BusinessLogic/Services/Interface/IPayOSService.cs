namespace BusinessLogic.Services.Interface
{
    public interface IPayOSService
    {
        Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
            long orderCode,
            decimal amountVnd,
            string description,
            CancellationToken cancellationToken = default);
        bool VerifyWebhookSignature(string receivedSignature, System.Text.Json.JsonElement dataElement);
    }

    public class PayOSPaymentLinkResult
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
    }
}
