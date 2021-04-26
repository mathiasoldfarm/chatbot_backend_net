using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace chatbot_backend.Controllers.Users {
    [ApiController]
    [Route("users/send-verification-code")]
    public class SendVerification : ControllerBase {
        public class Data {
            public string email { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                SendResetLink.SendVerificationLink(data.email, "verification", "verify");
                return Ok();
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
