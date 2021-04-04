using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;

namespace chatbot_backend.Controllers.Bot {
    [ApiController]
    [Route("bot/getanswer")]
    public class GetAnswer : ControllerBase {
        [HttpPost]
        public IActionResult Post(int contextId, int userId, int courseId, int sessionGroup, string type, string question) {
            try {
                string botQuery = generateBotQuery();

                return Ok();
            }
            catch (Exception e) {
                return BadRequest($"Error: {e}");
            }
        }

        private string generateBotQuery() {
            return "hej";
        }
    }
}
