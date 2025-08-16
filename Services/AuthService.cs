using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MyCabs.Api.DTOs;
using MyCabs.Api.Models;
using MyCabs.Api.Repositories;

namespace MyCabs.Api.Services
{
    public class AuthService
    {
        private readonly MongoContext _ctx;
        private readonly IConfiguration _config;

        public AuthService(MongoContext ctx, IConfiguration config)
        {
            _ctx = ctx;
            _config = config;
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            if (!Enum.TryParse<RoleType>(dto.Role, true, out var role))
                throw new ArgumentException("Invalid role");

            var existingEmail = await _ctx.Users
                .Find(u => u.Email!.ToLower() == dto.Email.ToLower())
                .FirstOrDefaultAsync();
            if (existingEmail != null)
                throw new InvalidOperationException("Email already registered");

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hash,
                Role = role,
                IsApproved = role == RoleType.User // Company/Driver/Admin cần duyệt
            };

            await _ctx.Users.InsertOneAsync(user);
        }

        // public async Task<string> LoginAsync(LoginDto dto)
        // {
        //     var user = await _ctx.Users
        //         .Find(u => u.Email == dto.Email)
        //         .FirstOrDefaultAsync();

        //     if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        //         throw new UnauthorizedAccessException("Invalid credentials");

        //     if ((user.Role == RoleType.Company || user.Role == RoleType.Driver) && !user.IsApproved)
        //         throw new UnauthorizedAccessException("Account not approved yet");

        //     return GenerateJwt(user);
        // }

        public async Task<(User user, string token)> LoginAsync(LoginDto dto)
        {
            var user = await _ctx.Users
                .Find(u => u.Email == dto.Email)
                .FirstOrDefaultAsync();



            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            if ((user.Role == RoleType.Company || user.Role == RoleType.Driver) && !user.IsApproved)
                throw new UnauthorizedAccessException("Account not approved yet");

            var token = GenerateJwt(user);
            return (user, token);
        }


        private string GenerateJwt(User user)
        {
            var secret = _config["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT secret is not configured.");
            var issuer = _config["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var audience = _config["JwtSettings:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
            var expiryMinutes = int.TryParse(_config["JwtSettings:ExpiryMinutes"], out var mm) ? mm : 1440;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim("role", user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _ctx.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) throw new InvalidOperationException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Current password incorrect");

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            var update = Builders<User>.Update.Set(u => u.PasswordHash, newHash);
            await _ctx.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UpdateAccountAsync(string userId, UpdateAccountDto dto)
        {
            var update = Builders<User>.Update.Set(u => u.Email, dto.Email);
            await _ctx.Users.UpdateOneAsync(u => u.Id == userId, update);
        }
    }
}
