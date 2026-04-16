namespace BusinessLogic.PaymentGateways;

public sealed class PaymentGatewayDescriptor
{
    public PaymentGatewayDescriptor(
        string code,
        string displayNameVi,
        IReadOnlyList<PaymentCredentialFieldDescriptor>? credentialFields = null)
    {
        Code = code;
        DisplayNameVi = displayNameVi;
        CredentialFields = credentialFields ?? Array.Empty<PaymentCredentialFieldDescriptor>();
    }

    public string Code { get; }
    public string DisplayNameVi { get; }
    public IReadOnlyList<PaymentCredentialFieldDescriptor> CredentialFields { get; }
}
