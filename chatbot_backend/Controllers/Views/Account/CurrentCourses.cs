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
    [Route("views/account/courses")]
    public class CurrentCourses : ControllerBase {
        [HttpGet]
        public async Task<IActionResult> Get() {
            string email = string.Empty;
            if (HttpContext.User.Identity is ClaimsIdentity identity) {
                email = identity.FindFirst(ClaimTypes.Email).Value;
            }

            Subjects subjects = new Subjects();
            return await subjects.Get(email);
        }
    }
}
