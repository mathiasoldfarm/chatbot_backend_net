using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Route("views/bøger")]
    public class Subjects : ControllerBase {
        private class CourseStatusData {
            public string title {
                get; private set;
            }

            public float status {
                get; private set;
            }

            public CourseStatusData(string _title, float _status) {
                title = _title;
                status = _status;
            }
        }
        private class CategoryData {
            public string title {
                get; private set;
            }

            public string color {
                get; private set;
            }

            public List<CourseStatusData> courses {
                get; private set;
            }

            public CategoryData(string _color, string _title) {
                title = _title;
                color = _color;
                courses = new List<CourseStatusData>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string givenEmail = null) {
            try {
                string fetchDataQuery = "";
                Dictionary<string, CategoryData> categories = new Dictionary<string, CategoryData>();

                if (givenEmail != null || HttpContext.User.Identity.IsAuthenticated) {
                    fetchDataQuery = @"
                        SELECT
                        courses.title as course_title,
                        categories.title as category,
                        categories.color_class as color,
                        COUNT(
	                        CASE WHEN exists(select * from users_sections_done where ""user"" = (

                                SELECT ""id"" FROM users WHERE email = @email
	                        ) and ""section"" = courses_sections.section_id ) THEN 1 END
                        )::float / COUNT(*)::float as status
                        FROM courses
                        INNER JOIN categories ON categories.id = courses.category
                        LEFT OUTER JOIN courses_sections ON courses_sections.course_id = courses.id
                        GROUP BY(courses.title, categories.title, categories.color_class)
                    ";

                    string email = string.Empty;
                    if ( givenEmail != null ) {
                        fetchDataQuery += @"
                        HAVING COUNT(
	                        CASE WHEN exists(select * from users_sections_done where ""user"" = (
                                SELECT ""id"" FROM users WHERE email = @email
	                        ) and ""section"" = courses_sections.section_id ) THEN 1 END
                        )::float / COUNT(*)::float <> 0
                        ";
                        email = givenEmail;
                    } else {
                        if (HttpContext.User.Identity is ClaimsIdentity identity) {
                            email = identity.FindFirst(ClaimTypes.Email).Value;
                        }
                    }

                    await using (var cmd = new NpgsqlCommand(fetchDataQuery, DB.connection)) {
                        cmd.Parameters.AddWithValue("email", email);
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                string course = (string)reader[0];
                                string category = (string)reader[1];
                                string color = (string)reader[2];
                                float status = Convert.ToSingle(reader[3]);

                                if (!categories.ContainsKey(category)) {
                                    categories[category] = new CategoryData(color, category);
                                }
                                categories[category].courses.Add(new CourseStatusData(course, status));
                            }
                        }
                    }

                    return Ok(categories.Values.ToList());
                }

                fetchDataQuery = @"
                SELECT courses.title, categories.title, categories.color_class
                FROM courses
                INNER JOIN categories ON categories.id = courses.category";

                await using (var cmd = new NpgsqlCommand(fetchDataQuery, DB.connection)) {
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            string course = (string)reader[0];
                            string category = (string)reader[1];
                            string color = (string)reader[2];

                            if (!categories.ContainsKey(category)) {
                                categories[category] = new CategoryData(color, category);
                            }
                            categories[category].courses.Add(new CourseStatusData(course, 0));
                        }
                    }
                }

                return Ok(categories.Values.ToList());
            } catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
