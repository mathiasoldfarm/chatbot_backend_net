using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using Npgsql;
using chatbot_backend.Controllers.Users;
using System.Collections.Generic;

namespace chatbot_backend.Controllers {
    [ApiController]
    [Route("users/create")]
    public class CreateUser : ControllerBase {
        public class Data {
             public string email { get; set; }
            public string password { get; set; }
            public string password2 {
                get; set;
            }
        }

        public static readonly string EmailNotValid = "Email is not valid.";
        public static readonly string AtLeast1CharacterError = "The password must contain at least one character";
        public static readonly string AtLeast1LowerCaseCharacterError = "The password must contain at least one lowercase character";
        public static readonly string AtLeast1UpperCaseCharacterError = "The password must contain at least one uppercase character";
        public static readonly string AtLeast1digitError = "The password must contain at least one digit";
        public static readonly string AtLeast1SpecialCharacterError = "The password must contain at least one special character";
        public static readonly string AtLeast8CharactersLongError = "The password must be at least 8 characters long";
        public static readonly string PasswordsNotMatch = "Passwords doesn't match";
        public static readonly string EmailAlreadyExists = "The email was already registered. Please try to log in.";

        public class Response {
            public string Type {
                get; set;
            }
            public string Message {
                get; set;
            }

            public Response(Exception e) {
                Message = e.Message;
                switch (Message) {
                    case "Email is not valid.":
                        Type = "email";
                        break;
                    case "The password must contain at least one character":
                        Type = "password";
                        break;
                    case "The password must contain at least one lowercase character":
                        Type = "password";
                        break;
                    case "The password must contain at least one uppercase character":
                        Type = "password";
                        break;
                    case "The password must contain at least one digit":
                        Type = "password";
                        break;
                    case "The password must contain at least one special character":
                        Type = "password";
                        break;
                    case "The password must be at least 8 characters long":
                        Type = "password";
                        break;
                    case "Passwords doesn't match":
                        Type = "password2";
                        break;
                    case "The email was already registered. Please try to log in.":
                        Type = "email";
                        break;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data ) {
            try {
                string email = data.email;
                string password = data.password;
                string password2 = data.password2;

                TestEmail(email);
                TestPassword(password);
                TestPassword2(password, password2);
                CreateUserIfNotExists(email, password);

                await SendResetLink.SendVerificationLink(data.email, "verification", "verify", "verifying your account", "account-verification");
                return Ok("User was succesfully created. Check your e-mail for confirmation.");
            } catch (Exception e) {
                return BadRequest(new Response(e));
            }
        }

        public static void TestEmail(string email) {
            string pattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
            Regex rg = new Regex(pattern);
            if ( !rg.IsMatch(email)) {
                throw new Exception(EmailNotValid);
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
                throw new Exception(AtLeast1CharacterError);
            } else if ( !atLeast1LowerCaseCharacter ) {
                throw new Exception(AtLeast1LowerCaseCharacterError);
            } else if ( !atLeast1UpperCaseCharacter ) {
                throw new Exception(AtLeast1UpperCaseCharacterError);
            } else if ( !atLeast1digit ) {
                throw new Exception(AtLeast1digitError);
            } else if ( !atLeast1SpecialCharacter ) {
                throw new Exception(AtLeast1SpecialCharacterError);
            } else if ( !atLeast8CharactersLong ) {
                throw new Exception(AtLeast8CharactersLongError);
            }
        }

        public static void TestPassword2(string password, string password2) {
            if (password != password2) {
                throw new Exception(PasswordsNotMatch);
            }
        }

        private void CreateUserIfNotExists(string email, string password) {
            try {
                string insertQuery = @"INSERT INTO users(email, password) VALUES(LOWER(@email), @password)";
                NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, DB.connection);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", Hashing.Compute(password));
                cmd.ExecuteNonQuery();
            } catch(Exception) {
                throw new Exception(EmailAlreadyExists);
            }
        }
    }
}