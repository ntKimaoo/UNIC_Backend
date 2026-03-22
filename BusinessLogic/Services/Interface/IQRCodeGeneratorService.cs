namespace BusinessLogic.Services.Interface
{
    public interface IQRCodeGeneratorService
    {
        byte[]? GetQrCodePngBytes(string content);
    }
}
