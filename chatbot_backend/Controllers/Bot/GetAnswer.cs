using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace chatbot_backend.Controllers.Bot {
    [ApiController]
    [Route("bot/getanswer")]
    public class GetAnswer : ControllerBase {
        private enum RequestType
        {
            Question,
            NoType
        }
        private readonly string ChatbotUrlBase = "http://localhost:5000";
        private class ChatbotResponse
        {
            string Answer { get; set; }
            List<string> NextPossibleAnswers { get; set; }
            RequestType Type { get; set; }
            string HistoryRequest { get; set; }

            public ChatbotResponse(string answer, List<string> nextPossibleAnswers, int type, string historyRequest)
            {
                Answer = answer;
                NextPossibleAnswers = nextPossibleAnswers;
                Type = (RequestType)type;
                HistoryRequest = historyRequest;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(int userId, int courseId, int contextId, int historyId, int _type, string question) {
            try {
                RequestType type = (RequestType)_type;
                string botQuery = await GenerateBotQuery(contextId, historyId, type, question);
                ChatbotResponse response = await ChatbotRequest(botQuery);

                return Ok();
            }
            catch (Exception e) {
                return BadRequest($"Error: {e}");
            }
        }

        // Generating query for chatbot service
        private async Task<string> GenerateBotQuery(int contextId, int historyId, RequestType type, string question) {
            string context = await GetContext(type, contextId);
            string history = await GetHistory(historyId);

            string url = $"{ChatbotUrlBase}/getanswer";
            url += $"/type:{type}";
            url += $"/question:{question}";
            url += $"/context:{context}";

            return url;
        }

        // Fetching sessions id's by history id
        private async Task<string> GetHistory(int historyId)
        {
            string fetchQuery = @"
            SELECT id
            FROM sessions
            WHERE history_id = @historyId;";

            List<int> history = new List<int>();
            await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection))
            {
                cmd.Parameters.AddWithValue("historyId", historyId);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int sessionId = (int)reader[0];
                        history.Add(sessionId);
                    }
                }
            }

            return JsonConvert.SerializeObject(history);
        }

        // Generating pairs of question and correct answer for quizzes by context id
        private async Task<string> GetQuestionCorrectAnswersByContextId(int contextId)
        {
            string fetchQuery = @"
            SELECT question_id, correct
            FROM sessions
            INNER JOIN course_sections ON course_sections.id = sessions.section_id
            INNER JOIN course_quiz_levels ON course_quiz_levels.quiz_id = course_sections.quiz_id AND course_quiz_levels.level = sessions.level
            INNER JOIN course_level_questions ON course_level_questions.quiz_level_id = course_quiz_levels.id
            INNER JOIN course_questions ON course_questions.id = course_level_questions.question_id
            WHERE context_id = @contextId";

            Dictionary<int, int> questionCorrectAnswers = new Dictionary<int, int>();
            await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection))
            {
                cmd.Parameters.AddWithValue("contextId", contextId);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int questionId = (int)reader[0];
                        int correctAnswerId = (int)reader[1];
                        questionCorrectAnswers.Add(questionId, correctAnswerId);
                    }
                }
            }

            return JsonConvert.SerializeObject(questionCorrectAnswers);
        }

        // Fetching context giving request type
        private async Task<string> GetContext(RequestType type, int contextId)
        {
            if (type == RequestType.Question)
            {
                return await GetQuestionCorrectAnswersByContextId(contextId);
            }
            return JsonConvert.SerializeObject(null);
        }

        private async Task<ChatbotResponse> ChatbotRequest(string query)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(query);

            if ( !response.IsSuccessStatusCode )
            {
                throw new Exception("Chatbot request failed!");
            }

            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ChatbotResponse>(content);
        }
    }
}
