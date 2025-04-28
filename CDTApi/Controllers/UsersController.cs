using CDTApi.DTOs;
using CDTApi.Models;
using CDTApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt; // For JwtSecurityTokenHandler
using System.Security.Claims; // For ClaimsIdentity and Claim
using System.Text; // For Encoding
using Microsoft.IdentityModel.Tokens; // For SecurityTokenDescriptor and SigningCredentials
namespace CDTApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IDAFRepository _dafRepo;

        public UsersController(IUserRepository userRepo, IDAFRepository dafRepo)
        {
            _userRepo = userRepo;
            _dafRepo = dafRepo;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterUserDTO dto)
        {
            if (_userRepo.GetByEmail(dto.Email) is not null)
                return BadRequest("Email already exists.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                RegistrationSource = dto.RegistrationSource,
                Mobile = dto.Mobile,
                Location = dto.Location
            };

            _userRepo.AddUser(user); // UserId generated inside

            // If the registration is for a DAF account, create the DAF record
            if (dto.RegistrationSource == "DAF")
            {
                int newId = _dafRepo.GetByUserId(user.UserId) == null
                    ? _userRepo.GetAllUsers().Count() + 1
                    : _dafRepo.GetByUserId(user.UserId)!.DAFAccountId;

                string accountNumber = $"DAF-{newId:D3}";

                var dafAccount = new DAFAccount
                {
                    DAFAccountId = newId,
                    UserId = user.UserId,
                    AccountNumber = accountNumber,
                    DAFBalance = 0,
                    TotalDonated = 0
                };

                _dafRepo.AddDAFAccount(dafAccount);

                return Ok(new
                {
                    message = "DAF user registered successfully",
                    dafAccountNumber = accountNumber,
                    userId = user.UserId
                });
            }

            return Ok(new { message = "User registered successfully", userId = user.UserId });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO dto)
        {
            var user = _userRepo.GetByEmail(dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            user.Token = CreateJWTToken(user);

            return Ok(new
            {
                token = user.Token,
                message = "Login successful",
                userId = user.UserId,
                name = user.Name,
                registrationSource = user.RegistrationSource
            });
        }

        private string CreateJWTToken(User user)
        {
            var jwtHandler = new JwtSecurityTokenHandler(); // Correct variable name
            var key = Encoding.ASCII.GetBytes("a-string-secret-at-least-256-bits-long");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            });
        
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddMinutes(1),
                SigningCredentials = credentials
            };
        
            var token = jwtHandler.CreateToken(tokenDescriptor); // Use jwtHandler
            return jwtHandler.WriteToken(token); // Use jwtHandler
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            return Ok(_userRepo.GetAllUsers());
        }
    }
}
