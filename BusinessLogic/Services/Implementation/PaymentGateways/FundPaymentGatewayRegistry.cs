using System.Linq;
using BusinessLogic.PaymentGateways;
using BusinessLogic.Services.Interface;

namespace BusinessLogic.Services.Implementation.PaymentGateways;

public sealed class FundPaymentGatewayRegistry : IFundPaymentGatewayRegistry
{
    private readonly Dictionary<string, IFundPaymentGateway> _byCode;

    public FundPaymentGatewayRegistry(IEnumerable<IFundPaymentGateway> gateways)
    {
        _byCode = gateways.ToDictionary(
            g => PaymentGatewayProviderCodes.Normalize(g.ProviderCode),
            StringComparer.OrdinalIgnoreCase);
    }

    public IFundPaymentGateway Get(string providerCode)
    {
        var key = PaymentGatewayProviderCodes.Normalize(providerCode);
        if (!_byCode.TryGetValue(key, out var gateway))
        {
            throw new InvalidOperationException(
                $"Cổng thanh toán trực tuyến '{providerCode}' chưa được hỗ trợ. Các mã hợp lệ: {string.Join(", ", _byCode.Keys.OrderBy(x => x))}.");
        }

        return gateway;
    }

    public IReadOnlyList<PaymentGatewayDescriptor> ListOnlineProviders() =>
        _byCode.Values
            .Select(g => new PaymentGatewayDescriptor(
                PaymentGatewayProviderCodes.Normalize(g.ProviderCode),
                g.DisplayNameVi,
                g.CredentialFields))
            .OrderBy(d => d.Code)
            .ToList();
}
