﻿using System;
using Microsoft.AspNetCore.Mvc;
using chatbot_backend.Models;
using System.Threading.Tasks;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Authorize]
    [Route("views/learn/{courseTitle}")]
    public class Course : ControllerBase {
        private class SectionData {
            public int id {
                get; private set;
            }

            public string name {
                get; private set;
            }

            public int parent {
                get; private set;
            }

            public int order {
                get; private set;
            }

            public bool done {
                get; private set;
            }

            public List<SectionData> children {
                get; private set;
            }

            public SectionData(int _id, string _name, int _parent, int _order, bool _done) : this(_id, _name, _order, _done) {
                parent = _parent;
            }

            public SectionData(int _id, string _name, int _order, bool _done) {
                id = _id;
                name = _name;
                order = _order;
                done = _done;
                parent = -1;
                children = new List<SectionData>();
            }

            public void SortChildren() {
                children = children.OrderBy(s => s.order).ToList();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string courseTitle) {
            try {
                string userEmail = "";
                if (HttpContext.User.Identity is ClaimsIdentity identity) {
                    userEmail = (string)identity.FindFirst(ClaimTypes.Email).Value;
                }

                string fetchQuery = @"
                SELECT sections.id, section_name, parent_id, courses_sections.order, users_sections_done.""user"" AS user_done
                FROM courses
                INNER JOIN courses_sections ON courses_sections.course_id = courses.id
                INNER JOIN sections ON courses_sections.section_id = sections.id
                LEFT OUTER JOIN users_sections_done ON users_sections_done.""user"" = (
                    SELECT ""id"" FROM users WHERE email = @userEmail
                ) AND users_sections_done.""section"" = sections.id
                WHERE LOWER(courses.title) = @course_title
                ORDER BY courses_sections.order";


                Dictionary<int, SectionData> sectionsDict = new Dictionary<int, SectionData>();
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("course_title", courseTitle);
                    cmd.Parameters.AddWithValue("userEmail", userEmail);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            int sectionId = (int)reader[0];
                            string sectionName = (string)reader[1];
                            int order = (int)reader[3];
                            bool done = reader[4] != DBNull.Value;

                            SectionData section;
                            if (reader[2] != DBNull.Value) {
                                int parentSectionId = (int)reader[2];
                                section = new SectionData(sectionId, sectionName, parentSectionId, order, done);
                            } else {
                                section = new SectionData(sectionId, sectionName, order, done);
                            }

                            sectionsDict.Add(sectionId, section);
                        }
                    }
                }

                foreach(SectionData section in sectionsDict.Values) {
                    if (section.parent != -1) {
                        sectionsDict[section.parent].children.Add(section);
                    }
                }

                foreach (SectionData section in sectionsDict.Values) {
                    section.SortChildren();
                }

                List<SectionData> sections = sectionsDict.Values.Where(s => s.parent == -1).ToList();
                sections = sections.OrderBy(s => s.order).ToList();

                return Ok(sections);
            }
            catch (Exception e) {
                return BadRequest(e.ToString());
            }
        }
    }
}
