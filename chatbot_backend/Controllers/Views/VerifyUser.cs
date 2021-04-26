using System;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using System.Threading.Tasks;
using Npgsql;

namespace chatbot_backend.Controllers.Views {
    [ApiController]

    [Route("views/verify/{userId}/{verificationCode}")]
    public class VerifyUser : ControllerBase {
        [HttpGet]
        public async Task<IActionResult> Get(int userId, string verificationCode) {
            try {
                ResetPassword.verifyUserAndCode(userId, verificationCode, "verification", "verification code");

                string updateVerifiedQuery = "UPDATE users SET verified = true WHERE id = @userId";
                NpgsqlCommand updateCmd = new NpgsqlCommand(updateVerifiedQuery, DB.connection);
                updateCmd.Parameters.AddWithValue("userId", userId);
                updateCmd.ExecuteNonQuery();

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
