using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string toEmail, string verificationToken, string fullName);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string fullName);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName);
    }
}
