using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.DTOs
{
    public class EmailQueueItem
    {
        public string ToEmail { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Token { get; set; }
        public EmailType EmailType { get; set; }
        public int RetryCount { get; set; } = 0;
    }

    public enum EmailType
    {
        Verification,
        PasswordReset,
        Welcome
    }
}
