using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Route("views")]
    public class Welcome : ControllerBase {
        private class CategoryData {
            public string color {
                get; private set;
            }

            public List<string> courses {
                get; private set;
            }

            public CategoryData(string _color) {
                color = _color;
                courses = new List<string>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get() {
            try {
                string fetchQuery = @"
                SELECT courses.title, categories.title, categories.color_class
                FROM courses
                INNER JOIN categories ON categories.id = courses.category";

                Dictionary<string, CategoryData> categories = new Dictionary<string, CategoryData>();
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            string course = (string)reader[0];
                            string category = (string)reader[1];
                            string color = (string)reader[2];

                            if (!categories.ContainsKey(category)) {
                                categories[category] = new CategoryData(color);
                            }
                            categories[category].courses.Add(course);
                        }
                    }
                }

                return Ok(categories);
            }
            catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
