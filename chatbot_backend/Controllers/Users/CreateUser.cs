using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using Npgsql;
using System.Security.Cryptography;
using System.Text.Json;

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
            string email = data.email;
            string password = data.password;
            if ( !TestEmail(email )) {
                return BadRequest("Email is not correct.");
            }
            if ( !TestPassword(password)) {
                return BadRequest("Password is not correct.");
            }
            bool succesfullyCreatedUser = await CreateUserIfNotExists(email, password);
            if ( succesfullyCreatedUser ) {
                return Ok("User was succesfully created. Check your e-mail for confirmation.");
            } else {
                return BadRequest("The email was already registered. Please try to log in.");
            }
        }

        private bool TestEmail(string email) {
            string pattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(email);
        }

        private bool TestPassword(string password) {
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[\+!@#\$%\^&\*])(?=.{8,})";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(password);
        }

        private async Task<bool> CreateUserIfNotExists(string email, string password) {
            try {
                string insertQuery = @"INSERT INTO users(email, password) VALUES(@email, @password)";
                NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, DB.connection);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", ComputeHash(password));
                cmd.ExecuteNonQuery();

                return true;
            } catch(Exception e) {
                return false;
            }
        }

        private string ComputeHash(string password) {
            using (SHA256 sha256Hash = SHA256.Create()) {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password)); 
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}