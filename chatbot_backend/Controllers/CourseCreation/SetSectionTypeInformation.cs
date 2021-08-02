using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.Threading.Tasks;

namespace chatbot_backend.Controllers.CourseCreation {
    [ApiController]
    [Authorize]
    [Route("coursecreation/set_section_type_information")]
    public class SetSectionTypeInformation : ControllerBase {
        public class Data {
            public int sectionId {
                get; set;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                // TRYING TO GET DESCRIPTION ID
                int descriptionId = -1;

                string insertQuery = $@"SELECT description_id FROM sections WHERE id = @sectionId";
                try {
                    await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                        cmd.Parameters.AddWithValue("sectionId", data.sectionId);
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                if (reader[0] != DBNull.Value) {
                                    descriptionId = (int)reader[0];
                                }
                            }
                        }
                    }
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                if (descriptionId == -1) {
                    insertQuery = $@"INSERT INTO descriptions VALUES(default) RETURNING ""id""";
                    try {
                        await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                            await using (var reader = await cmd.ExecuteReaderAsync()) {
                                while (await reader.ReadAsync()) {
                                    if (reader[0] != DBNull.Value) {
                                        descriptionId = (int)reader[0];
                                    }
                                }
                            }
                        }
                    } catch (NpgsqlOperationInProgressException e) {
                        Thread.Sleep(1000);
                    }

                    insertQuery = $@"INSERT INTO description_levels(""level"", description_id) VALUES(1, @descriptionId)";
                    NpgsqlCommand insertCmd = new NpgsqlCommand(insertQuery, DB.connection);
                    insertCmd.Parameters.AddWithValue("descriptionId", descriptionId);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // SETTING DESCRIPTION_ID AND USING_DESCRIPTION
                string updateQuery = $@"UPDATE sections
                SET using_quiz = false, using_description = true, description_id = @descriptionId
                WHERE id = @sectionId;";

                NpgsqlCommand updateCmd = new NpgsqlCommand(updateQuery, DB.connection);
                updateCmd.Parameters.AddWithValue("sectionId", data.sectionId);
                updateCmd.Parameters.AddWithValue("descriptionId", descriptionId);
                updateCmd.ExecuteNonQuery();

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }
    }
}
