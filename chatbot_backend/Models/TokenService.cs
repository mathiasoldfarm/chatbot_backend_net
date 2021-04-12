using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace chatbot_backend.Models {
    public static class TokenService {
        private const double EXPIRE_HOURS = 12.0;
        public static string CreateToken(string email) {
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("SECRET"));
            var tokenHandler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, email),
                }),
                Expires = DateTime.UtcNow.AddHours(EXPIRE_HOURS),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(descriptor);
            return "Bearer " + tokenHandler.WriteToken(token);
        }
    }
}
