using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using chatbot_backend.Controllers.Utils;
using chatbot_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Route("views/header")]
    public class Private : ControllerBase {
        private class Route {
            public string url;
            public string name;

            public Route(string url, string name) {
                this.url = url;
                this.name = name;
            }
        }

        private class Routes {
            public List<Route> contentRoutes {
                get; set;
            }
            public List<Route> userRoutes {
                get; set;
            }

            public Routes(int role, string userType) {
                contentRoutes = new List<Route>();
                userRoutes = new List<Route>();

                if (role == 1) {
                    // TODO: Handle
                } else {
                    if (userType == "teacher") {
                        contentRoutes.Add(new Route("/bøger", "Bøger"));
                        userRoutes.Add(new Route("/elever", "Elever"));
                        userRoutes.Add(new Route("/konto", "Konto"));
                    } else if (userType == "student") {
                        contentRoutes.Add(new Route("/bøger", "Bøger"));
                        userRoutes.Add(new Route("/klasse", "Klasse"));
                        userRoutes.Add(new Route("/konto", "Konto"));
                    }
                }
            }

            public Routes() {
                contentRoutes = new List<Route>();
                userRoutes = new List<Route>();
                contentRoutes.Add(new Route("/bøger", "Bøger"));
                userRoutes.Add(new Route("/opret-bruger", "Opret bruger"));
            }
        }
        [HttpGet]
        public async Task<IActionResult> Get() {
            try {
                if (HttpContext.User.Identity.IsAuthenticated) {
                    string email = string.Empty;
                    if (HttpContext.User.Identity is ClaimsIdentity identity) {
                        email = identity.FindFirst(ClaimTypes.Email).Value;
                    }

                    int role = -1;
                    string userType = "";
                    string fetchQuery = @"
                        SELECT role, (
	                        case 
		                        when exists (
			                        SELECT user_id
			                        FROM teachers
			                        WHERE user_id = users.id
		                        ) 
		                        then 'teacher'
		                        when exists (
			                        SELECT user_id
			                        FROM students
			                        WHERE user_id = users.id
		                        ) 
	                           then 'student'
	                           else ''
	                        end
                        ) as user_type
                        FROM users
                        WHERE email = LOWER(@email);
                    ";
                    await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                        cmd.Parameters.AddWithValue("email", email);
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                role = Unpacker.UnpackDBIntValue(0, reader);
                                userType = Unpacker.UnpackDBStringValue(1, reader);
                            }
                        }
                    }

                    return Ok(new Routes(role, userType));
                }
                return Ok(new Routes());
            } catch(Exception e) {
                return BadRequest(e.Message);
            }
        }
    }
}
