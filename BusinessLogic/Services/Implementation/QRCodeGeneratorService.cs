using BusinessLogic.Services.Interface;
using QRCoder;

namespace BusinessLogic.Services.Implementation
{
    public class QRCodeGeneratorService : IQRCodeGeneratorService
    {
        public byte[]? GetQrCodePngBytes(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                return qrCode.GetGraphic(4);
            }
            catch
            {
                return null;
            }
        }
    }
}
