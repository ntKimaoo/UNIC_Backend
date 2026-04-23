namespace BusinessLogic.PaymentGateways;

public sealed class PaymentCredentialFieldDescriptor
{
    public PaymentCredentialFieldDescriptor(
        string name,
        string labelVi,
        bool requiredWhenEnabled,
        int maxLength,
        string inputType = "password",
        string? helpTextVi = null,
        int sortOrder = 0)
    {
        Name = name;
        LabelVi = labelVi;
        RequiredWhenEnabled = requiredWhenEnabled;
        MaxLength = maxLength;
        InputType = inputType;
        HelpTextVi = helpTextVi;
        SortOrder = sortOrder;
    }

    public string Name { get; }
    public string LabelVi { get; }
    public bool RequiredWhenEnabled { get; }
    public int MaxLength { get; }
    public string InputType { get; }
    public string? HelpTextVi { get; }
    public int SortOrder { get; }

    public static class FieldNames
    {
        public const string ClientId = "clientId";
        public const string ApiKey = "apiKey";
        public const string ChecksumKey = "checksumKey";
    }
}
