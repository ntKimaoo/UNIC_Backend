namespace BusinessLogic.PaymentGateways;

public static class PaymentGatewayProviderCodes
{
    public const string PayOS = "PAYOS";
    public const string VNPay = "VNPAY";

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return PayOS;
        return code.Trim().ToUpperInvariant();
    }
}
