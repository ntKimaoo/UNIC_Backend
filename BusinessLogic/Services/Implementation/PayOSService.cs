using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLogic.Options;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Options;

namespace BusinessLogic.Services.Implementation
{
    public class PayOSService : IPayOSService
    {
        private readonly HttpClient _httpClient;
        private readonly PayOSOptions _options;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public PayOSService(HttpClient httpClient, IOptions<PayOSOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(
            long orderCode,
            decimal amountVnd,
            string description,
            CancellationToken cancellationToken = default)
        {
            var amountInt = (int)Math.Round(amountVnd);
            if (amountInt <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(amountVnd));

            if (_options.UseMock)
            {
                var mockUrl = BuildMockCheckoutUrl(orderCode);
                return new PayOSPaymentLinkResult
                {
                    CheckoutUrl = mockUrl,
                    QrCode = mockUrl,
                    PaymentLinkId = $"mock-{orderCode}"
                };
            }

            if (string.IsNullOrEmpty(_options.ClientId) || string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.ChecksumKey))
                throw new InvalidOperationException("PayOS chưa được cấu hình (ClientId, ApiKey, ChecksumKey). Bật PayOS:UseMock=true trong appsettings.Development.json để test local không cần credential.");

            var desc = description?.Trim() ?? "Nop quy";
            if (desc.Length > 255) desc = desc[..255];

            var returnUrl = _options.ReturnUrl.TrimEnd('/');
            var cancelUrl = _options.CancelUrl.TrimEnd('/');

            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(_options.LinkExpirationMinutes).ToUnixTimeSeconds();

            var dataStr = $"amount={amountInt}&cancelUrl={cancelUrl}&description={desc}&orderCode={orderCode}&returnUrl={returnUrl}";
            var signature = ComputeHmacSha256Hex(_options.ChecksumKey, dataStr);

            var body = new
            {
                orderCode,
                amount = amountInt,
                description = desc,
                cancelUrl,
                returnUrl,
                expiredAt,
                signature
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "v2/payment-requests");
            request.Headers.Add("x-client-id", _options.ClientId);
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayOS API lỗi: {response.StatusCode}. {content}");

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (code != "00")
            {
                var descErr = root.TryGetProperty("desc", out var d) ? d.GetString() : content;
                throw new InvalidOperationException($"PayOS: {descErr}");
            }

            var data = root.GetProperty("data");
            var checkoutUrl = data.TryGetProperty("checkoutUrl", out var u) ? u.GetString() ?? "" : "";
            var qrCode = data.TryGetProperty("qrCode", out var q) ? q.GetString() ?? "" : "";
            var paymentLinkId = data.TryGetProperty("paymentLinkId", out var p) ? p.GetString() ?? "" : "";

            return new PayOSPaymentLinkResult
            {
                CheckoutUrl = checkoutUrl,
                QrCode = qrCode,
                PaymentLinkId = paymentLinkId
            };
        }

        private string BuildMockCheckoutUrl(long orderCode)
        {
            var template = _options.MockCheckoutUrlTemplate?.Trim();
            if (!string.IsNullOrEmpty(template))
            {
                return template
                    .Replace("{transactionId}", orderCode.ToString(), StringComparison.Ordinal)
                    .Replace("{orderCode}", orderCode.ToString(), StringComparison.Ordinal);
            }

            var ret = _options.ReturnUrl?.Trim();
            if (!string.IsNullOrEmpty(ret))
                return $"{ret.TrimEnd('/')}?mockTransactionId={orderCode}";

            return $"about:blank#mock-pay-txn-{orderCode}";
        }

        public bool VerifyWebhookSignature(string receivedSignature, JsonElement dataElement)
        {
            if (string.IsNullOrEmpty(_options.ChecksumKey) || string.IsNullOrEmpty(receivedSignature))
                return false;

            var dataStr = BuildSortedKeyValueString(dataElement);
            var computed = ComputeHmacSha256Hex(_options.ChecksumKey, dataStr);
            return string.Equals(computed, receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Build string key1=value1&key2=value2... từ data webhook (key sort alphabet).
        /// Giá trị null/undefined -> "". Nested object/array: JSON raw text (theo mẫu PayOS).
        /// </summary>
        private static string BuildSortedKeyValueString(JsonElement data)
        {
            var parts = new List<string>();
            foreach (var prop in data.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                var v = prop.Value;
                string val;
                if (v.ValueKind == JsonValueKind.Object || v.ValueKind == JsonValueKind.Array)
                    val = v.GetRawText();
                else if (v.ValueKind == JsonValueKind.Null || v.ValueKind == JsonValueKind.Undefined)
                    val = "";
                else
                    val = v.ToString() ?? "";
                parts.Add($"{prop.Name}={val}");
            }
            return string.Join("&", parts);
        }

        private static string ComputeHmacSha256Hex(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
