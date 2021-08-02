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
    [Route("coursecreation/add_question")]
    public class AddQuestion : ControllerBase {
        public class Data {
            public int sectionId {
                get; set;
            }
        }

        public class ResponseData {
            public int questionId {
                get; set;
            }

            public int answerId {
                get; set;
            }

            public ResponseData(int answerId, int questionId) {
                this.answerId = answerId;
                this.questionId = questionId;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            try {

                int questionId = -1;
                int answerId = -1;

                // INSERTING QUESTION
                string insertQuery = $@"INSERT INTO questions (question, correct) VALUES('', 0) RETURNING id";
                try {
                    await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                questionId = (int)reader[0];
                            }
                        }
                    }
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                // INSERTING ANSWER
                insertQuery = $@"INSERT INTO answers(answer, explanation) VALUES('', '') RETURNING id";
                try {
                    await using (var cmd = new NpgsqlCommand(insertQuery, DB.connection)) {
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                answerId = (int)reader[0];
                            }
                        }
                    }
                } catch (NpgsqlOperationInProgressException e) {
                    Thread.Sleep(1000);
                }

                // INSERTING QUIZ_LEVEL_QUESTION relation and question_answer_relation
                insertQuery = $@"
                    INSERT INTO quiz_levels_questions(quiz_level_id, question_id, ""order"")
                    VALUES(
                        (SELECT quiz_levels.id FROM sections
                        INNER JOIN quiz_levels ON quiz_levels.quiz_id = sections.quiz_id
                        WHERE sections.id = @sectionId),
	                    @questionId,
	                    (
                            WITH current_order AS(
                                SELECT MAX(orders.order) +1 FROM(
                               SELECT quiz_levels_questions.order FROM sections

                               INNER JOIN quiz_levels ON quiz_levels.quiz_id = sections.quiz_id

                               INNER JOIN quiz_levels_questions ON quiz_levels_questions.quiz_level_id = (
                               (SELECT quiz_levels.id FROM sections

                               INNER JOIN quiz_levels ON quiz_levels.quiz_id = sections.quiz_id

                               WHERE sections.id = @sectionId))
			                    WHERE sections.id = @sectionId
			                    ) as orders
		                    )
		                    SELECT
                            CASE
                                WHEN NOT EXISTS(SELECT * FROM current_order) THEN 0
                                WHEN(SELECT * FROM current_order) IS NULL THEN 0
                                ELSE(SELECT * FROM current_order)

                            END
	                    )
                    );
                    INSERT INTO questions_answers(question_id, possible_answer_id, ""order"")
                    VALUES (
	                    @questionId,
	                    @answerId,
	                    (
		                    WITH current_order AS(
			                    SELECT MAX(orders.order) +1 FROM (
				                    SELECT ""order""
                                    FROM questions_answers
                                    WHERE question_id = @questionId
			                    ) as orders
		                    )
		                    SELECT
                            CASE
                                WHEN NOT EXISTS(SELECT * FROM current_order) THEN 0
                                WHEN(SELECT * FROM current_order) IS NULL THEN 0
                                ELSE(SELECT * FROM current_order)
                            END
	                    )
                    );
                ";
                NpgsqlCommand insertCmd = new NpgsqlCommand(insertQuery, DB.connection);
                insertCmd.Parameters.AddWithValue("sectionId", data.sectionId);
                insertCmd.Parameters.AddWithValue("questionId", questionId);
                insertCmd.Parameters.AddWithValue("answerId", answerId);
                insertCmd.ExecuteNonQuery();

                return Ok(new ResponseData(answerId, questionId));
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }
    }
}
