using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLogic.Options;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class PayOSServiceTest
    {
        private static PayOSOptions BaseOptions(Action<PayOSOptions>? configure = null)
        {
            var o = new PayOSOptions
            {
                ReturnUrl = "https://app.test/return/",
                CancelUrl = "https://app.test/cancel/",
                LinkExpirationMinutes = 45,
                UseMock = false
            };
            configure?.Invoke(o);
            return o;
        }

        private static PayOSMerchantCredential Merchant(Action<PayOSMerchantCredential>? configure = null)
        {
            var m = new PayOSMerchantCredential
            {
                ClientId = "client-id",
                ApiKey = "api-key",
                ChecksumKey = "checksum-secret"
            };
            configure?.Invoke(m);
            return m;
        }

        private static PayOSService CreateSut(HttpClient http, PayOSOptions options) =>
            new PayOSService(http, Options.Create(options));

        private sealed class CallbackHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

            public CallbackHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
                _send = send;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _send(request, cancellationToken);
        }

        private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((req, _) => Task.FromResult(responder(req)))
                .Verifiable();
            return new HttpClient(handler.Object) { BaseAddress = new Uri("https://api-merchant.payos.vn/") };
        }

        private static HttpClient CreateAsyncHttpClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
            new HttpClient(new CallbackHttpMessageHandler(sendAsync))
            {
                BaseAddress = new Uri("https://api-merchant.payos.vn/")
            };

        private static string HmacSha256Hex(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
        }

        #region CreatePaymentLinkAsync

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldThrow_WhenAmountZero()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o => o.UseMock = true));

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CreatePaymentLinkAsync(Merchant(), 10, 0m, "desc", default));
            Assert.Contains("Số tiền", ex.Message);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldThrow_WhenAmountNegative()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o => o.UseMock = true));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.CreatePaymentLinkAsync(Merchant(), 10, -1m, "desc", default));
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldRoundAmount_ToInt()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o => o.UseMock = true));

            var r = await sut.CreatePaymentLinkAsync(Merchant(), 7, 100.6m, "d", default);

            Assert.Contains("7", r.CheckoutUrl, StringComparison.Ordinal);
            Assert.Equal("mock-7", r.PaymentLinkId);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_UseMock_ShouldUseTemplate_WhenSet()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o =>
            {
                o.UseMock = true;
                o.MockCheckoutUrlTemplate = "https://pay.test/{orderCode}/t/{transactionId}";
            }));

            var r = await sut.CreatePaymentLinkAsync(Merchant(), 42, 5000m, "x", default);

            Assert.Equal("https://pay.test/42/t/42", r.CheckoutUrl);
            Assert.Equal("https://pay.test/42/t/42", r.QrCode);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_UseMock_ShouldAppendQuery_WhenReturnUrlOnly()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o =>
            {
                o.UseMock = true;
                o.MockCheckoutUrlTemplate = "";
                o.ReturnUrl = "https://return.test/cb/";
            }));

            var r = await sut.CreatePaymentLinkAsync(Merchant(), 99, 1000m, "x", default);

            Assert.Equal("https://return.test/cb?mockTransactionId=99", r.CheckoutUrl);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_UseMock_ShouldUseAboutBlank_WhenNoReturnUrlNorTemplate()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o =>
            {
                o.UseMock = true;
                o.MockCheckoutUrlTemplate = "";
                o.ReturnUrl = "";
                o.CancelUrl = "";
            }));

            var r = await sut.CreatePaymentLinkAsync(Merchant(), 3, 1000m, "x", default);

            Assert.Equal("about:blank#mock-pay-txn-3", r.CheckoutUrl);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldThrow_WhenNotMockAndMissingCredentials()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions(o =>
            {
                o.UseMock = false;
            }));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CreatePaymentLinkAsync(Merchant(m => m.ClientId = ""), 1, 1000m, "x", default));
            Assert.Contains("PayOS", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldReturnParsedData_WhenApiSuccess()
        {
            var json = """
                       {"code":"00","data":{"checkoutUrl":"https://checkout","qrCode":"qr-data","paymentLinkId":"pl_abc"}}
                       """;
            using var http = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
            var sut = CreateSut(http, BaseOptions());

            var r = await sut.CreatePaymentLinkAsync(Merchant(), 55, 10_000m, "  Nộp quỹ  ", default);

            Assert.Equal("https://checkout", r.CheckoutUrl);
            Assert.Equal("qr-data", r.QrCode);
            Assert.Equal("pl_abc", r.PaymentLinkId);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldUseDefaultDescription_WhenNull()
        {
            string? capturedBody = null;
            using var http = CreateAsyncHttpClient(async (req, ct) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"code":"00","data":{"checkoutUrl":"u","qrCode":"q","paymentLinkId":"p"}}""",
                        Encoding.UTF8,
                        "application/json")
                };
            });
            var sut = CreateSut(http, BaseOptions());

            await sut.CreatePaymentLinkAsync(Merchant(), 1, 1000m, null!, default);

            Assert.NotNull(capturedBody);
            Assert.Contains("Nop quy", capturedBody, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldTruncateDescription_WhenLongerThan255()
        {
            string? capturedBody = null;
            using var http = CreateAsyncHttpClient(async (req, ct) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"code":"00","data":{"checkoutUrl":"u","qrCode":"q","paymentLinkId":"p"}}""",
                        Encoding.UTF8,
                        "application/json")
                };
            });
            var sut = CreateSut(http, BaseOptions());
            var longDesc = new string('x', 300);

            await sut.CreatePaymentLinkAsync(Merchant(), 1, 1000m, longDesc, default);

            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            Assert.Equal(255, doc.RootElement.GetProperty("description").GetString()!.Length);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldThrow_WhenHttpError()
        {
            using var http = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad", Encoding.UTF8, "text/plain")
            });
            var sut = CreateSut(http, BaseOptions());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CreatePaymentLinkAsync(Merchant(), 1, 1000m, "d", default));
            Assert.Contains("BadRequest", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreatePaymentLinkAsync_ShouldThrow_WhenCodeNotSuccess()
        {
            using var http = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":"99","desc":"Sai thông tin"}""", Encoding.UTF8, "application/json")
            });
            var sut = CreateSut(http, BaseOptions());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CreatePaymentLinkAsync(Merchant(), 1, 1000m, "d", default));
            Assert.Contains("Sai thông tin", ex.Message);
        }

        #endregion

        #region VerifyWebhookSignature

        [Fact]
        public void VerifyWebhookSignature_ReturnsFalse_WhenChecksumKeyEmpty()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions());
            using var doc = JsonDocument.Parse("""{"orderCode":1}""");

            Assert.False(sut.VerifyWebhookSignature("", "abc", doc.RootElement));
        }

        [Fact]
        public void VerifyWebhookSignature_ReturnsFalse_WhenReceivedSignatureEmpty()
        {
            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions());
            using var doc = JsonDocument.Parse("""{"orderCode":1}""");

            Assert.False(sut.VerifyWebhookSignature("k", "", doc.RootElement));
            Assert.False(sut.VerifyWebhookSignature("k", "   ", doc.RootElement));
        }

        [Fact]
        public void VerifyWebhookSignature_ReturnsTrue_WhenHmacMatchesSortedKeys()
        {
            const string key = "webhook-key";
            // Sorted: amount before orderCode
            var payload = """{"orderCode":5,"amount":1000}""";
            var dataStr = "amount=1000&orderCode=5";
            var sig = HmacSha256Hex(key, dataStr);

            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions());
            using var doc = JsonDocument.Parse(payload);

            Assert.True(sut.VerifyWebhookSignature(key, sig, doc.RootElement));
        }

        [Fact]
        public void VerifyWebhookSignature_IsCaseInsensitive_ForHex()
        {
            const string key = "k";
            using var doc = JsonDocument.Parse("""{"a":1}""");
            var dataStr = "a=1";
            var sigLower = HmacSha256Hex(key, dataStr);
            var sigUpper = sigLower.ToUpperInvariant();

            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions());

            Assert.True(sut.VerifyWebhookSignature(key, sigUpper, doc.RootElement));
        }

        [Fact]
        public void VerifyWebhookSignature_ReturnsFalse_WhenTampered()
        {
            const string key = "k";
            using var doc = JsonDocument.Parse("""{"a":1}""");
            var badSig = HmacSha256Hex(key, "a=2");

            using var http = new HttpClient(new HttpClientHandler());
            var sut = CreateSut(http, BaseOptions());

            Assert.False(sut.VerifyWebhookSignature(key, badSig, doc.RootElement));
        }

        #endregion
    }
}
