namespace BusinessLogic.Services.Interface
{
    /// <summary>
    /// Generates QR code images (e.g. for check-in tokens in emails).
    /// </summary>
    public interface IQRCodeGeneratorService
    {
        /// <summary>
        /// Returns PNG bytes for a QR code encoding the given content.
        /// </summary>
        byte[]? GetQrCodePngBytes(string content);
    }
}
