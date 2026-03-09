using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace UNIC.BusinessLogic.Test.Services
{
    public class JwtServiceTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly JwtService _jwtService;

        public JwtServiceTest()
        {
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVerySecretKeyThatMustBeAtLeast32BytesLong!");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

            _jwtService = new JwtService(_mockConfig.Object);
        }

        [Fact]
        public void GenerateAccessToken_ShouldReturnValidJwtToken()
        {
            var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", FullName = "Test User" };
            var token = _jwtService.GenerateAccessToken(user);

            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnBase64String()
        {
            var token = _jwtService.GenerateRefreshToken();

            Assert.False(string.IsNullOrEmpty(token));
            // Check if valid base64
            var bytes = Convert.FromBase64String(token);
            Assert.Equal(64, bytes.Length);
        }

        [Fact]
        public void ValidateAccessToken_ShouldReturnUserId_WhenTokenContainsIntSub()
        {
            // Note: In JwtService, ValidateAccessToken tries to parse the 'sub' claim as an integer.
            // If User.UserId is a Guid, this method as implemented might fail to parse.
            // But we test the public method with a manually created token containing an integer 'sub'.

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("ThisIsAVerySecretKeyThatMustBeAtLeast32BytesLong!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "123")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var result = _jwtService.ValidateAccessToken(tokenString);

            Assert.Equal(123, result);
        }

        [Fact]
        public void ValidateAccessToken_ShouldReturnNull_WhenSubIndInvalidFormat()
        {
            // Providing a Guid, which cannot be parsed to int, to trigger the failure branch.
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("ThisIsAVerySecretKeyThatMustBeAtLeast32BytesLong!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var result = _jwtService.ValidateAccessToken(tokenString);

            Assert.Null(result);
        }

        [Fact]
        public void ValidateAccessToken_ShouldReturnNull_WhenTokenInvalid()
        {
            var result = _jwtService.ValidateAccessToken("invalid-token-string");
            
            Assert.Null(result);
        }
    }
}
