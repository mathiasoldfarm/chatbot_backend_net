using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using Npgsql;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Route("users/login")]
    public class LogIn : ControllerBase {
        public class Data {
            public string email { get; set; }
            public string password { get; set; }
        }
        public class Response {
            public string Message { get; set; }
            public string Token { get; set; }

            public Response(string message, string token) {
                Message = message;
                Token = token;
            }
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                string email = data.email;
                string password = data.password;

                CreateUser.TestEmail(email);

                string fetchedEmail = "";
                string fetchedPasswordHashed = "";
                bool verified = false;
                string fetchQuery = "SELECT email, password, verified FROM users WHERE email = @email;";
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("email", email);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            fetchedEmail = (string)reader[0];
                            fetchedPasswordHashed = (string)reader[1];
                            verified = (bool)reader[2];
                        }
                    }
                }
                if (fetchedEmail == "") {
                    throw new Exception("The email could not be found");
                }
                if (Hashing.Compute(password) != fetchedPasswordHashed) {
                    throw new Exception("The password was not correct for the given account.");
                }
                if (!verified) {
                    throw new Exception("The user has not been verified yet. Try to request a new verification link.");
                }
                string token = TokenService.CreateToken(email);
                return Ok(new Response("You was succesfully authenticated", token));
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}