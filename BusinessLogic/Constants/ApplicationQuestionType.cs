namespace UNIC.BusinessLogic.Constants
{
    public static class ApplicationQuestionType
    {
        public const string Text = "TEXT";
        public const string Essay = "ESSAY";
        public const string MultipleChoice = "MULTIPLE_CHOICE";

        public static readonly IReadOnlySet<string> ValidTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Text,
            Essay,
            MultipleChoice
        };

        public static bool IsValid(string? questionType)
        {
            return !string.IsNullOrWhiteSpace(questionType) && ValidTypes.Contains(questionType);
        }
    }
}
