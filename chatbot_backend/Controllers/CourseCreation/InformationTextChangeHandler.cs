using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

namespace chatbot_backend.Controllers.CourseCreation {
    [ApiController]
    [Authorize]
    [Route("coursecreation/information_text_change_handler")]
    public class InformationTextChangeHandler : ControllerBase {
        public class Data {
            public int informationTextId {
                get; set;
            }
            public string newText {
                get; set;
            }
        }

        [HttpPost]
        public IActionResult Post([FromBody] Data data) {
            try {
                
                 string updateQuery = $@"
                UPDATE description_levels
                SET description = @text
                WHERE id = @id;";
                NpgsqlCommand cmd = new NpgsqlCommand(updateQuery, DB.connection);
                cmd.Parameters.AddWithValue("text", data.newText);
                cmd.Parameters.AddWithValue("id", data.informationTextId);
                try {
                    cmd.ExecuteNonQuery();
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }
    }
}
