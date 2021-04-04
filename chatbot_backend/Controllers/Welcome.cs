using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Route("")]
    public class Welcome : ControllerBase {
        [HttpGet]
        public string Get() {
            return "Welcome to the API :-)";
        }
    }
}
