using BusinessLogic.Services.Implementation;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class QRCodeGeneratorServiceTest
    {
        private readonly QRCodeGeneratorService _service = new();

        [Fact]
        public void GetQrCodePngBytes_ReturnsNull_WhenContentNull()
        {
            Assert.Null(_service.GetQrCodePngBytes(null!));
        }

        [Fact]
        public void GetQrCodePngBytes_ReturnsNull_WhenContentWhitespace()
        {
            Assert.Null(_service.GetQrCodePngBytes("   "));
        }

        [Fact]
        public void GetQrCodePngBytes_ReturnsPngBytes_WhenContentValid()
        {
            var bytes = _service.GetQrCodePngBytes("https://pay.test/checkout/123");

            Assert.NotNull(bytes);
            Assert.True(bytes!.Length > 50);
            // PNG signature
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'N', bytes[2]);
            Assert.Equal((byte)'G', bytes[3]);
        }

        [Fact]
        public void GetQrCodePngBytes_ReturnsDeterministicSize_ForSameContent()
        {
            const string content = "uni-club-fund-txn-99";
            var a = _service.GetQrCodePngBytes(content);
            var b = _service.GetQrCodePngBytes(content);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.Equal(a!.Length, b!.Length);
        }
    }
}
