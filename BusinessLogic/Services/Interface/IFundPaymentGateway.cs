using System.Collections.Generic;
using System.Text.Json;
using BusinessLogic.PaymentGateways;
using DataAccess.Models;

namespace BusinessLogic.Services.Interface;

public interface IFundPaymentGateway
{
    string ProviderCode { get; }
    string DisplayNameVi { get; }
    IReadOnlyList<PaymentCredentialFieldDescriptor> CredentialFields { get; }
    void ValidateCredentialsForSave(ClubPayOSSettings settings);
    void ValidateForCreatingPaymentLink(ClubPayOSSettings settings);
    Task<ContributionPaymentLinkResult> CreatePaymentLinkAsync(
        ClubPayOSSettings settings,
        long orderCode,
        decimal amountVnd,
        string description,
        CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(ClubPayOSSettings settings, string? receivedSignature, JsonElement dataElement);
}

public sealed class ContributionPaymentLinkResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string ExternalPaymentId { get; set; } = string.Empty;
}
