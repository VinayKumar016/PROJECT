using CDTApi.DTOs;
using CDTApi.Models;
using CDTApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CDTApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonationsController : ControllerBase
    {
        private readonly IDonationRepository _donationRepo;
        private readonly IDAFRepository _dafRepo;

        public DonationsController(IDonationRepository donationRepo, IDAFRepository dafRepo)
        {
            _donationRepo = donationRepo;
            _dafRepo = dafRepo;
        }

        [HttpPost("add")]
        public IActionResult AddDonation([FromBody] DonationRequestDTO dto)
        {
            var daf = _dafRepo.GetByUserId(dto.UserId);
            if (daf == null)
                return NotFound("DAF account not found.");

            if (dto.Amount <= 0 || dto.Amount > daf.DAFBalance)
                return BadRequest("Invalid or insufficient donation amount.");

            var donation = new Donation
            {
                UserId = dto.UserId,
                CharityId = dto.CharityId,
                Amount = dto.Amount
            };

            _donationRepo.AddDonation(donation);
            _dafRepo.UpdateBalance(daf.DAFAccountId, dto.Amount, isDonation: true);

            return Ok(new { message = "Donation successful", donationId = donation.DonationId });
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserDonations(int userId)
        {
            var list = _donationRepo.GetDonationsByUserId(userId);
            return Ok(list);
        }
    }
}
