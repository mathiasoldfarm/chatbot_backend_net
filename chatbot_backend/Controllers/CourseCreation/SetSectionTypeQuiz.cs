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
    [Route("coursecreation/set_section_type_quiz")]
    public class SetSectionTypeQuiz : ControllerBase {
        public class Data {
            public int sectionId {
                get; set;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {
                // TRYING TO GET QUIZID
                int quizId = -1;

                string insertQuery = $@"SELECT quiz_id FROM sections WHERE id = @sectionId";
                try {
                    await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                        cmd.Parameters.AddWithValue("sectionId", data.sectionId);
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                if (reader[0] != DBNull.Value) {
                                    quizId = (int)reader[0];
                                }
                            }
                        }
                    }
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                if ( quizId == -1 ) {
                    insertQuery = $@"INSERT INTO quizzes VALUES(default) RETURNING ""id""";
                    try {
                        await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                            await using (var reader = await cmd.ExecuteReaderAsync()) {
                                while (await reader.ReadAsync()) {
                                    if (reader[0] != DBNull.Value) {
                                        quizId = (int)reader[0];
                                    }
                                }
                            }
                        }
                    } catch (NpgsqlOperationInProgressException e) {
                        Thread.Sleep(1000);
                    }

                    insertQuery = $@"INSERT INTO quiz_levels(""level"", quiz_id) VALUES(1, @quizId)";
                    NpgsqlCommand insertQuizCmd = new NpgsqlCommand(insertQuery, DB.connection);
                    insertQuizCmd.Parameters.AddWithValue("quizId", quizId);
                    await insertQuizCmd.ExecuteNonQueryAsync();
                }

                // SETTING QUIZ_ID AND USING_QUIZ
                string updateQuery = $@"UPDATE sections
                SET using_quiz = true, using_description = false, quiz_id = @quizId
                WHERE id = @sectionId;";

                NpgsqlCommand updateCmd = new NpgsqlCommand(updateQuery, DB.connection);
                updateCmd.Parameters.AddWithValue("sectionId", data.sectionId);
                updateCmd.Parameters.AddWithValue("quizId", quizId);
                updateCmd.ExecuteNonQuery();

                return Ok();
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }
    }
}
