using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Authorize]
    [Route("users/field/update")]
    public class UpdateField : ControllerBase {
        public class Data {
            public string field { get; set; }
            public string value { get; set; }
        }

        [HttpPost]
        public IActionResult Post([FromBody] Data data) {
            try {
                string email = string.Empty;
                if (HttpContext.User.Identity is ClaimsIdentity identity) {
                    email = identity.FindFirst(ClaimTypes.Email).Value;
                }
                string field = data.field;
                string value = data.value;

                string updateQuery = $@"
                UPDATE users
                SET {field} = @value
                WHERE email = @email";
                NpgsqlCommand cmd = new NpgsqlCommand(updateQuery, DB.connection);
                cmd.Parameters.AddWithValue("value", value);
                cmd.Parameters.AddWithValue("email", email);
                try {
                    cmd.ExecuteNonQuery();
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
