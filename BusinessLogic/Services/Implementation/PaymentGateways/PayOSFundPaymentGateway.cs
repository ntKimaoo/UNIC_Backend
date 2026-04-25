using System.Collections.Generic;
using System.Text.Json;
using BusinessLogic.Options;
using BusinessLogic.PaymentGateways;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using Microsoft.Extensions.Options;

namespace BusinessLogic.Services.Implementation.PaymentGateways;

public sealed class PayOSFundPaymentGateway : IFundPaymentGateway
{
    private readonly IPayOSService _payOSService;
    private readonly PayOSOptions _options;

    public PayOSFundPaymentGateway(IPayOSService payOSService, IOptions<PayOSOptions> options)
    {
        _payOSService = payOSService;
        _options = options.Value;
    }

    public string ProviderCode => PaymentGatewayProviderCodes.PayOS;

    public string DisplayNameVi => "PayOS";

    public IReadOnlyList<PaymentCredentialFieldDescriptor> CredentialFields { get; } =
    [
        new(
            PaymentCredentialFieldDescriptor.FieldNames.ClientId,
            "Client ID (PayOS)",
            requiredWhenEnabled: true,
            maxLength: 100,
            inputType: "text",
            helpTextVi: "Lấy trên dashboard PayOS sau khi đăng ký merchant.",
            sortOrder: 0),
        new(
            PaymentCredentialFieldDescriptor.FieldNames.ApiKey,
            "API Key",
            requiredWhenEnabled: true,
            maxLength: 200,
            inputType: "password",
            helpTextVi: null,
            sortOrder: 1),
        new(
            PaymentCredentialFieldDescriptor.FieldNames.ChecksumKey,
            "Checksum Key",
            requiredWhenEnabled: true,
            maxLength: 200,
            inputType: "password",
            helpTextVi: "Dùng để xác thực webhook PayOS.",
            sortOrder: 2)
    ];

    public void ValidateCredentialsForSave(ClubPayOSSettings settings)
    {
        if (!settings.IsEnabled)
            return;
        if (_options.UseMock)
            return;
        if (string.IsNullOrWhiteSpace(settings.ClientId)
            || string.IsNullOrWhiteSpace(settings.ApiKey)
            || string.IsNullOrWhiteSpace(settings.ChecksumKey))
        {
            throw new ArgumentException("Bật PayOS thì cần đủ ClientId, ApiKey, ChecksumKey.");
        }
    }

    public void ValidateForCreatingPaymentLink(ClubPayOSSettings settings)
    {
        if (_options.UseMock)
            return;

        if (settings == null || !settings.IsEnabled)
            throw new InvalidOperationException("Câu lạc bộ chưa bật thanh toán trực tuyến hoặc chưa cấu hình.");

        if (!string.Equals(
                PaymentGatewayProviderCodes.Normalize(settings.PaymentProvider),
                PaymentGatewayProviderCodes.PayOS,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cấu hình cổng thanh toán không khớp PayOS.");
        }

        if (string.IsNullOrWhiteSpace(settings.ClientId)
            || string.IsNullOrWhiteSpace(settings.ApiKey)
            || string.IsNullOrWhiteSpace(settings.ChecksumKey))
        {
            throw new InvalidOperationException("PayOS của câu lạc bộ chưa được cấu hình đầy đủ (ClientId/ApiKey/ChecksumKey).");
        }
    }

    public async Task<ContributionPaymentLinkResult> CreatePaymentLinkAsync(
        ClubPayOSSettings settings,
        long orderCode,
        decimal amountVnd,
        string description,
        CancellationToken cancellationToken = default)
    {
        ValidateForCreatingPaymentLink(settings);

        PayOSMerchantCredential merchant;
        if (_options.UseMock)
        {
            merchant = new PayOSMerchantCredential
            {
                ClientId = "mock",
                ApiKey = "mock",
                ChecksumKey = "mock"
            };
        }
        else
        {
            merchant = new PayOSMerchantCredential
            {
                ClientId = settings.ClientId.Trim(),
                ApiKey = settings.ApiKey.Trim(),
                ChecksumKey = settings.ChecksumKey.Trim()
            };
        }

        var r = await _payOSService.CreatePaymentLinkAsync(merchant, orderCode, amountVnd, description, cancellationToken);
        return new ContributionPaymentLinkResult
        {
            CheckoutUrl = r.CheckoutUrl,
            QrCode = r.QrCode,
            ExternalPaymentId = r.PaymentLinkId
        };
    }

    public bool VerifyWebhookSignature(ClubPayOSSettings settings, string? receivedSignature, JsonElement dataElement)
    {
        if (string.IsNullOrWhiteSpace(settings.ChecksumKey))
            return false;
        if (string.IsNullOrEmpty(receivedSignature))
            return false;
        return _payOSService.VerifyWebhookSignature(settings.ChecksumKey.Trim(), receivedSignature, dataElement);
    }
}
