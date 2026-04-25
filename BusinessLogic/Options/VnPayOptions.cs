namespace BusinessLogic.Options;
public class VnPayOptions
{
    public const string SectionName = "VnPay";
    public string PaymentBaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = string.Empty;
    public string DefaultIpAddr { get; set; } = "127.0.0.1";
}

