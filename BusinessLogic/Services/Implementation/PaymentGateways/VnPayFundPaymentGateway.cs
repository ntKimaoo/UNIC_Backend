using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

using BusinessLogic.Options;

using BusinessLogic.PaymentGateways;

using BusinessLogic.Services.Interface;

using DataAccess.Models;

using Microsoft.Extensions.Options;



namespace BusinessLogic.Services.Implementation.PaymentGateways;



public sealed class VnPayFundPaymentGateway : IFundPaymentGateway

{

    private readonly VnPayOptions _options;



    public VnPayFundPaymentGateway(IOptions<VnPayOptions> options)

    {

        _options = options.Value;

    }



    public string ProviderCode => PaymentGatewayProviderCodes.VNPay;



    public string DisplayNameVi => "VNPay";

    public IReadOnlyList<PaymentCredentialFieldDescriptor> CredentialFields { get; } =
    [
        new(
            PaymentCredentialFieldDescriptor.FieldNames.ClientId,
            "Mã website (TMN Code)",
            requiredWhenEnabled: true,
            maxLength: 100,
            inputType: "text",
            helpTextVi: "Mã định danh merchant trên cổng VNPay (sandbox/production).",
            sortOrder: 0),
        new(
            PaymentCredentialFieldDescriptor.FieldNames.ApiKey,
            "Hash Secret",
            requiredWhenEnabled: true,
            maxLength: 200,
            inputType: "password",
            helpTextVi: "Khóa ký giao dịch; không chia sẻ công khai.",
            sortOrder: 1)
    ];

    public void ValidateCredentialsForSave(ClubPayOSSettings settings)

    {

        if (!settings.IsEnabled)

            return;

        if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ApiKey))

            throw new ArgumentException("Bật VNPay thì cần TMN Code (ClientId) và Hash Secret (ApiKey).");

    }



    public void ValidateForCreatingPaymentLink(ClubPayOSSettings settings)

    {

        if (settings == null || !settings.IsEnabled)

            throw new InvalidOperationException("Câu lạc bộ chưa bật thanh toán trực tuyến hoặc chưa cấu hình.");



        if (!string.Equals(

                PaymentGatewayProviderCodes.Normalize(settings.PaymentProvider),

                PaymentGatewayProviderCodes.VNPay,

                StringComparison.Ordinal))

        {

            throw new InvalidOperationException("Cấu hình cổng thanh toán không khớp VNPay.");

        }



        if (string.IsNullOrWhiteSpace(_options.ReturnUrl))

        {

            throw new InvalidOperationException(

                "Chưa cấu hình VnPay:ReturnUrl trên máy chủ (URL kết quả thanh toán đăng ký với VNPay).");

        }



        if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ApiKey))

            throw new InvalidOperationException("VNPay của câu lạc bộ chưa được cấu hình đầy đủ (TMN Code / Hash Secret).");

    }



    public Task<ContributionPaymentLinkResult> CreatePaymentLinkAsync(

        ClubPayOSSettings settings,

        long orderCode,

        decimal amountVnd,

        string description,

        CancellationToken cancellationToken = default)

    {

        ValidateForCreatingPaymentLink(settings);



        var tmn = settings.ClientId.Trim();

        var secret = settings.ApiKey.Trim();

        var orderInfo = string.IsNullOrWhiteSpace(description) ? "Nop quy" : description.Trim();

        if (orderInfo.Length > 240)

            orderInfo = orderInfo[..240];



        var amountMinor = (long)(amountVnd * 100m);

        if (amountMinor <= 0)

            throw new ArgumentException("Số tiền không hợp lệ cho VNPay.", nameof(amountVnd));



        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)

        {

            ["vnp_Version"] = "2.1.0",

            ["vnp_Command"] = "pay",

            ["vnp_TmnCode"] = tmn,

            ["vnp_Locale"] = "vn",

            ["vnp_CurrCode"] = "VND",

            ["vnp_TxnRef"] = orderCode.ToString(CultureInfo.InvariantCulture),

            ["vnp_OrderInfo"] = orderInfo,

            ["vnp_OrderType"] = "other",

            ["vnp_Amount"] = amountMinor.ToString(CultureInfo.InvariantCulture),

            ["vnp_ReturnUrl"] = _options.ReturnUrl.Trim(),

            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(_options.DefaultIpAddr) ? "127.0.0.1" : _options.DefaultIpAddr.Trim(),

            ["vnp_CreateDate"] = VnPaySignature.VietnamCreateDateUtcPlus7()

        };



        var url = VnPaySignature.BuildSignedPaymentUrl(_options.PaymentBaseUrl.Trim(), parameters, secret);

        return Task.FromResult(new ContributionPaymentLinkResult

        {

            CheckoutUrl = url,

            QrCode = url,

            ExternalPaymentId = orderCode.ToString(CultureInfo.InvariantCulture)

        });

    }



    public bool VerifyWebhookSignature(ClubPayOSSettings settings, string? receivedSignature, JsonElement dataElement)

    {

        _ = settings;

        _ = receivedSignature;

        _ = dataElement;

        return false;

    }

}

