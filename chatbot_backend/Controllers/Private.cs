using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Authorize]
    [Route("views/private")]
    public class Private : ControllerBase {
        [HttpGet]
        public string Get() {
            return "Here's a secret...";
        }
    }
}
