using System;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using System.Threading.Tasks;
using Npgsql;

namespace chatbot_backend.Controllers.Users {
    [ApiController]
    [Route("users/reset-password")]
    public class ResetPassword : ControllerBase {
        public class Data {
            public int userId { get; set; }
            public string oldPassword { get; set; }
            public string newPassword { get; set; }
            public string verificationCode { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                Views.ResetPassword.verifyUserAndCode(data.userId, data.verificationCode, "password_reset_request", "password-reset link");

                string fetchedPasswordHashed = "";
                string fetchQuery = "SELECT password FROM users WHERE id = @id;";
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("id", data.userId);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            fetchedPasswordHashed = (string)reader[1];
                        }
                    }
                }

                if (fetchedPasswordHashed != Hashing.Compute(data.oldPassword)) {
                    throw new Exception("Current password is not correct");
                }

                CreateUser.TestPassword(data.newPassword);

                string updateVerifiedQuery = "UPDATE users SET password = @password WHERE id = @userId";
                NpgsqlCommand updateCmd = new NpgsqlCommand(updateVerifiedQuery, DB.connection);
                updateCmd.Parameters.AddWithValue("password", data.newPassword);
                updateCmd.Parameters.AddWithValue("userId", data.userId);
                updateCmd.ExecuteNonQuery();

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }

    [Route("users/send-password-reset-link")]
    public class SendResetLink : ControllerBase {
        public class Data {
            public string email { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                SendVerificationLink(data.email, "password_reset_request", "reset-password");
                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }

        public async static void SendVerificationLink(string email, string table, string prefix) {
            Guid verification_code = Guid.NewGuid();

            string insertQuery = $@"
                    INSERT INTO {table}(""user"", verification_code, datetime)
                    VALUES(@email, @verification_code, @date);
                    SELECT id FROM users WHERE email = @email;
                ";

            int userId = -1;
            try {
                await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("email", email);
                    cmd.Parameters.AddWithValue("verification_code", verification_code);
                    cmd.Parameters.AddWithValue("date", DateTime.Now);

                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            userId = (int)reader[0];
                        }
                    }
                }
            } catch (PostgresException e) {
                if (e.SqlState == "23503") {
                    throw new Exception("User not found");
                } else {
                    throw new Exception("Operation failed");
                }
            }

            string link = Contants.frontend_url + $"/{prefix}/{userId}/{verification_code}";

            // TODO: Send link
        }
    }
}
