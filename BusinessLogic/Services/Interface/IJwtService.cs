using BusinessLogic.DTOs;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IJwtService
    {
        string GenerateAccessToken(Member member);
        string GenerateRefreshToken();
        int? ValidateAccessToken(string token);
    }
}
