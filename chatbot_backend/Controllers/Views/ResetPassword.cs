using System;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using System.Threading.Tasks;
using Npgsql;

namespace chatbot_backend.Controllers.Views {
    [Route("views/reset-password/{userId}/{verificationCode}")]
    public class ResetPassword : ControllerBase {
        [HttpGet]
        public async Task<IActionResult> Get(int userId, string verificationCode) {
            try {
                verifyUserAndCode(userId, verificationCode, "password_reset_request", "password-reset link");
                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }

        public static async void verifyUserAndCode(int userId, string verificationCode, string table, string name, int hoursLimit = 1) {
            // Fetch email from userId
            string selectQuery = @"SELECT email
                FROM users
                WHERE id = @userId";
            string email = "";

            await using (var cmd = new NpgsqlCommand(selectQuery, DB.connection)) {
                cmd.Parameters.AddWithValue("userId", userId);

                await using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        email = (string)reader[0];
                    }
                }
            }

            if (email == "") {
                throw new Exception("User not found from Id");
            }

            // Fetch date from email
            selectQuery = $@"SELECT datetime 
                FROM {table}
                WHERE ""user"" = @email
                AND verification_code = @verificationCode";

            DateTime? date = null;
            await using (var cmd = new NpgsqlCommand(selectQuery, DB.connection)) {
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("verificationCode", verificationCode);
                await using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        date = (DateTime)reader[0];
                    }
                }
            }
            if (date == null) {
                throw new Exception($"The {name} did not exist with the user");
            }

            // Verify date with now
            TimeSpan difference = DateTime.Now.Subtract(date.Value);
            if (difference.TotalHours >= hoursLimit) {
                throw new Exception($"The {name} expired. Try to resend and click on the link within {hoursLimit} hour.");
            }
        }
    }
}
