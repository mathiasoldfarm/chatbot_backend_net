using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using Npgsql;
using System.Security.Cryptography;
using System.Text.Json;

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
            string email = data.email;
            string password = data.password;
            if (!TestEmail(email)) {
                return BadRequest("Email is not correct.");
            }

            string fetchedEmail = "";
            string fetchedPasswordHashed = "";
            string fetchQuery = "SELECT email, password FROM users WHERE email = @email;";
            await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                cmd.Parameters.AddWithValue("email", email);
                await using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        fetchedEmail = (string)reader[0];
                        fetchedPasswordHashed = (string)reader[1];
                    }
                }
            }
            if (fetchedEmail == "") {
                return BadRequest("The email could not be found");
            }
            if (ComputeHash(password) != fetchedPasswordHashed) {
                return BadRequest("The password was not correct for the given account");
            }
            string token = TokenService.CreateToken(email);
            return Ok(new Response("You was succesfully authenticated", token));
        }

        private bool TestEmail(string email) {
            string pattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(email);
        }

        private string ComputeHash(string password) {
            using (SHA256 sha256Hash = SHA256.Create()) {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}