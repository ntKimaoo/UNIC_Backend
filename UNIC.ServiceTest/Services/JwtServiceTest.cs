using BusinessLogic.Services.Implementation;
using DataAccess.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace UNIC.ServiceTest.Services
{
    public class JwtServiceTest
    {
        private readonly JwtService _service;
        private readonly IConfiguration _configuration;

        public JwtServiceTest()
        {
            var configData = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ThisIsASuperSecretKeyForTestingPurposesOnly1234567890" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:ExpireMinutes", "60" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _service = new JwtService(_configuration);
        }

        #region GenerateAccessToken

        [Fact]
        public void GenerateAccessToken_ReturnsValidToken()
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@test.com",
                FullName = "Test User",
                UserRoles = new List<UserRole>(),
                ClubMembers = new List<UserClubRole>()
            };

            var token = _service.GenerateAccessToken(user);

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateAccessToken_ContainsCorrectClaims()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                Email = "test@test.com",
                FullName = "Test User",
                UserRoles = new List<UserRole> { new UserRole { RoleName = "Admin" } },
                ClubMembers = new List<UserClubRole>()
            };

            var token = _service.GenerateAccessToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal("test@test.com", jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        [Fact]
        public void GenerateAccessToken_IncludesClubRoles_WhenUserHasActiveClubMembers()
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@test.com",
                FullName = "Test User",
                UserRoles = new List<UserRole>(),
                ClubMembers = new List<UserClubRole>
                {
                    new UserClubRole
                    {
                        ClubId = 1,
                        Status = "Active",
                        ClubRole = new ClubRole { RoleName = "President", Level = 1 }
                    }
                }
            };

            var token = _service.GenerateAccessToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Contains(jwtToken.Claims, c => c.Type == "club_roles");
        }

        [Fact]
        public void GenerateAccessToken_NoClubRolesClaim_WhenNoActiveMembers()
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@test.com",
                FullName = "Test User",
                UserRoles = new List<UserRole>(),
                ClubMembers = new List<UserClubRole>
                {
                    new UserClubRole
                    {
                        ClubId = 1,
                        Status = "Inactive",
                        ClubRole = new ClubRole { RoleName = "Member", Level = 5 }
                    }
                }
            };

            var token = _service.GenerateAccessToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "club_roles");
        }

        #endregion

        #region GenerateRefreshToken

        [Fact]
        public void GenerateRefreshToken_ReturnsNonEmptyString()
        {
            var token = _service.GenerateRefreshToken();

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateRefreshToken_IsUnique()
        {
            var token1 = _service.GenerateRefreshToken();
            var token2 = _service.GenerateRefreshToken();

            Assert.NotEqual(token1, token2);
        }

        #endregion

        #region ValidateAccessToken

        [Fact]
        public void ValidateAccessToken_ReturnsNull_WhenTokenInvalid()
        {
            var result = _service.ValidateAccessToken("invalid-token-string");

            Assert.Null(result);
        }

        [Fact]
        public void ValidateAccessToken_ReturnsNull_WhenTokenFromDifferentKey()
        {
            // Generate token with different key
            var otherConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "ACompletelyDifferentSuperSecretKeyForTesting1234567890" },
                    { "Jwt:Issuer", "TestIssuer" },
                    { "Jwt:Audience", "TestAudience" },
                    { "Jwt:ExpireMinutes", "60" }
                })
                .Build();

            var otherService = new JwtService(otherConfig);
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@test.com",
                FullName = "Test",
                UserRoles = new List<UserRole>(),
                ClubMembers = new List<UserClubRole>()
            };

            var token = otherService.GenerateAccessToken(user);
            var result = _service.ValidateAccessToken(token);

            Assert.Null(result);
        }

        #endregion
    }
}
