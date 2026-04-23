using BusinessLogic.PaymentGateways;

namespace BusinessLogic.Services.Interface;

public interface IFundPaymentGatewayRegistry
{
    IFundPaymentGateway Get(string providerCode);

    IReadOnlyList<PaymentGatewayDescriptor> ListOnlineProviders();
}
