using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Authorize]
    [Route("views/account")]
    public class Private : ControllerBase {
        [HttpGet]
        public string Get() {
            string email = string.Empty;
            if (HttpContext.User.Identity is ClaimsIdentity identity) {
                email = identity.FindFirst(ClaimTypes.Email).Value;
            }
            return $"Here's a secret...{email}";
        }
    }
}
