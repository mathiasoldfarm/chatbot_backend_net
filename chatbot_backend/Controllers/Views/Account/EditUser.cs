using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Authorize]
    [Route("views/account/edit/user")]
    public class EditUser : ControllerBase {
        private class Response {
            public string firstname { get; set; }
            public string email { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> Get() {
            try {
                string email = string.Empty;
                if (HttpContext.User.Identity is ClaimsIdentity identity) {
                    email = identity.FindFirst(ClaimTypes.Email).Value;
                }

                string fetchQuery = @"
                SELECT firstname, email
                FROM users
                WHERE email = @email";

                Response response = new Response();
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("email", email);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            if (reader[0] != DBNull.Value) {
                                response.firstname = (string)reader[0];
                            }
                            if (reader[1] != DBNull.Value) {
                                response.email = (string)reader[1];
                            }
                        }
                    }
                }

                return Ok(response);
            } catch (Exception e) {
                return BadRequest($"Error: {e}");
            }
        }
    }
}
