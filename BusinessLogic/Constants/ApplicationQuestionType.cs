namespace UNIC.BusinessLogic.Constants
{
    /// <summary>
    /// Các loại câu hỏi trong form đơn ứng tuyển.
    /// Backend lưu câu trả lời qua AnswerText với quy ước:
    /// - TEXT, ESSAY: nội dung nhập trực tiếp.
    /// - MULTIPLE_CHOICE: giá trị đã chọn (label hoặc mã option do client gửi).
    /// - CHECKBOX (nếu dùng): nhiều lựa chọn, có thể lưu dạng "value1,value2" hoặc JSON.
    /// </summary>
    public static class ApplicationQuestionType
    {
        public const string Text = "TEXT";
        public const string Essay = "ESSAY";
        public const string MultipleChoice = "MULTIPLE_CHOICE";

        /// <summary>
        /// Các loại hợp lệ (để validate khi cần).
        /// </summary>
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
