using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using Npgsql;
using chatbot_backend.Controllers.Users;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Route("users/create")]
    public class CreateUser : ControllerBase {
        public class Data {
            public string email { get; set; }
            public string password { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data ) {
            try {
                string email = data.email;
                string password = data.password;

                TestEmail(email);
                TestPassword(password);

                CreateUserIfNotExists(email, password);

                SendResetLink.SendVerificationLink(data.email, "verification", "verify");
                return Ok("User was succesfully created. Check your e-mail for confirmation.");
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }

        public static void TestEmail(string email) {
            string pattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
            Regex rg = new Regex(pattern);
            if ( !rg.IsMatch(email)) {
                throw new Exception("Email is not valid.");
            }
        }

        public static void TestPassword(string password) {
            bool atLeast1Character = new Regex("(?=.*[A-z])").IsMatch(password);
            bool atLeast1LowerCaseCharacter = new Regex("(?=.*[a-z])").IsMatch(password);
            bool atLeast1UpperCaseCharacter = new Regex("(?=.*[A-Z])").IsMatch(password);
            bool atLeast1digit = new Regex("(?=.*[0-9])").IsMatch(password);
            bool atLeast1SpecialCharacter = new Regex("([^A-Za-z0-9])").IsMatch(password);
            bool atLeast8CharactersLong = new Regex("(?=.{8,})").IsMatch(password);

            if ( !atLeast1Character ) {
                throw new Exception("The password must contain at least one character");
            } else if ( !atLeast1LowerCaseCharacter ) {
                throw new Exception("The password must contain at least one lowercase character");
            } else if ( !atLeast1UpperCaseCharacter ) {
                throw new Exception("The password must contain at least one uppercase character");
            } else if ( !atLeast1digit ) {
                throw new Exception("The password must contain at least one digit");
            } else if ( !atLeast1SpecialCharacter ) {
                throw new Exception("The password must contain at least one special character");
            } else if ( !atLeast8CharactersLong ) {
                throw new Exception("The password must be at least 8 characters long");
            }
        }

        private void CreateUserIfNotExists(string email, string password) {
            try {
                string insertQuery = @"INSERT INTO users(email, password) VALUES(@email, @password)";
                NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, DB.connection);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", Hashing.Compute(password));
                cmd.ExecuteNonQuery();
            } catch(Exception e) {
                throw new Exception("The email was already registered. Please try to log in.");
            }
        }
    }
}