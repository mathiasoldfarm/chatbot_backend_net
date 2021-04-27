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
            public string password { get; set; }
            public string password2 {
                get; set;
            }
            public string verificationCode { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                await Views.ResetPassword.verifyUserAndCode(data.userId, data.verificationCode, "password_reset_request", "password-reset link");

                string fetchedPasswordHashed = "";
                string fetchQuery = "SELECT password FROM users WHERE id = @id;";
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("id", data.userId);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            fetchedPasswordHashed = (string)reader[0];
                        }
                    }
                }

                CreateUser.TestPassword2(data.password, data.password2);
                CreateUser.TestPassword(data.password);

                string updateVerifiedQuery = @"
                    UPDATE users SET password = @password WHERE id = @userId;
                    DELETE FROM password_reset_request WHERE ""user"" = (
                        SELECT email FROM users WHERE id = @userId
                    );
                ";

                NpgsqlCommand updateCmd = new NpgsqlCommand(updateVerifiedQuery, DB.connection);
                updateCmd.Parameters.AddWithValue("password", Hashing.Compute(data.password));
                updateCmd.Parameters.AddWithValue("userId", data.userId);
                updateCmd.ExecuteNonQuery();

                return Ok("Your password was succesfully changed.");
            } catch (Exception e) {
                return BadRequest(e.Message);
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
                await SendVerificationLink(data.email, "password_reset_request", "reset-password", "resetting your password", "password-reset");
                return Ok("A link has been sent to your inbox.");
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }

        public async static Task<bool> SendVerificationLink(string email, string table, string prefix, string mailTypeName, string mailLinkTitle) {
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

            Mailer.Send(
                $@"
                <p>Hi</p>
                <p>Here is your requested link for {mailTypeName}:</p>
                <a title=""{mailLinkTitle}"" href=""{link}"" target=""_blank"">{link}</a>
                <p>It can only be used once and within 1 hour.</p>
                ",
                email
            );

            return true;
        }
    }
}
