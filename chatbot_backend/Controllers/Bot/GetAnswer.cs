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
        private enum RequestType {
            NoType,
            Question,
            Section,
            Search
        }
        private enum DataFetchingRequest {
            NoType,
            FetchNextSection,
            FetchPreviousSection,
            FetchSectionById,
            SearchForContent
        }
        private enum FetchingDirection {
            Forward,
            Backward
        }
        private enum ContentTypes {
            Quiz,
            Description
        }
        private readonly string ChatbotUrlBase = "http://localhost:5000";
        private class ChatbotResponse
        {
            public string Answer { get; private set; }
            public List<string> NextPossibleAnswers { get; private set; }
            public DataFetchingRequest Type { get; private set; }
            public bool GetNewHistory { get; private set; }
            public int NextContextId {
                get; private set;
            }

            public void PickAnswer(int i) {
                string[] possibleAnswers = Answer.Split("|");
                if ( possibleAnswers.Length - 1 < i ) {
                    throw new Exception($"You can't pick an answer at index {i} of {possibleAnswers.Length} answers given by that chatbot service ");
                }

                Answer = possibleAnswers[i];
            }

            public ChatbotResponse(string answer, List<string> nextPossibleAnswers, int type, bool getNewHistory, int nextContextId)
            {
                Answer = answer;
                NextPossibleAnswers = nextPossibleAnswers;
                Type = (DataFetchingRequest)type;
                GetNewHistory = getNewHistory;
                NextContextId = nextContextId;
            }
        }
        private class ClientData {
            public Section Section {
                get; private set;
            }
            public int CourseId
            {
                get; private set;
            }
            public int ContextId {
                get; private set;
            }
            public string Answer {
                get; private set;
            }
            public int HistoryId {
                get; private set;
            }
            public List<string> NextPossibleAnswers {
                get; private set;
            }


            public ClientData(Section section, int courseId, ChatbotResponse botResponse, int historyId) {
                Section = section;
                ContextId = botResponse.NextContextId;
                Answer = botResponse.Answer;
                HistoryId = historyId;
                NextPossibleAnswers = botResponse.NextPossibleAnswers;
                CourseId = courseId;
            }
        }

        int CourseId { get; set; } = -1;
        int HistoryId { get; set; } = -1;
        string Question { get; set; }
        RequestType _Type { get; set; }
        ChatbotResponse botResponse { get; set; }

        [HttpPost]
        public async Task<IActionResult> Post(int userId, int courseId, int contextId, int initialHistoryId, int type, string question) {
            try {
                CourseId = courseId;
                _Type = (RequestType)type;
                Question = question;


                string botQuery = await GenerateBotQuery(contextId, initialHistoryId);
                botResponse = await ChatbotRequest(botQuery);

                HistoryId = await GetHistoryId(botResponse.GetNewHistory, initialHistoryId);

                Section section = await GetDataForResponseByRequestType(botResponse.Type);
                Session session = new Session(CourseId, section, Question, botResponse.Answer, botResponse.NextContextId, HistoryId);
                session.insert();

                return Ok(new ClientData(section, courseId, botResponse, HistoryId));
            }
            catch (Exception e) {
                return BadRequest($"Error: {e}");
            }
        }

        // Generating query for chatbot service
        private async Task<string> GenerateBotQuery(int contextId, int historyId) {
            string context = await GetContext(contextId, historyId);
            string history = await GetHistory(historyId);

            string url = $"{ChatbotUrlBase}/getanswer?";
            url += $"type={_Type.ToString().ToLower()}";
            url += $"&question={Question}";
            url += $"&context={context}";
            url += $"&contextId={contextId}";
            url += $"&history={history}";

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

            try {
                return JsonConvert.SerializeObject(history);
            } catch (Exception e) {
                throw new Exception($"Could not serialize history data: {e}");
            }
        }

        // Generating pairs of question and correct answer for quizzes by context id
        private async Task<string> GetQuestionCorrectAnswersByContextId(int contextId, int historyId)
        {
            string fetchQuery = @"
            SELECT question_id, correct
            FROM sessions
            INNER JOIN sections ON sections.id = sessions.section_id
            INNER JOIN quiz_levels ON quiz_levels.quiz_id = sections.quiz_id AND quiz_levels.level = sessions.level
            INNER JOIN quiz_levels_questions ON quiz_levels_questions.quiz_level_id = quiz_levels.id
            INNER JOIN questions ON questions.id = quiz_levels_questions.question_id
            WHERE context_id = @contextId AND history_id = @historyId";

            Dictionary<int, int> questionCorrectAnswers = new Dictionary<int, int>();
            await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection))
            {
                cmd.Parameters.AddWithValue("contextId", contextId);
                cmd.Parameters.AddWithValue("historyId", historyId);
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
            try {
                return JsonConvert.SerializeObject(questionCorrectAnswers);
            } catch ( Exception e) {
                throw new Exception($"Could not serialize question-correct answers relations: {e}");
            }
        }

        // Fetching context giving request type
        private async Task<string> GetContext(int contextId, int historyId)
        {
            if (_Type == RequestType.Question)
            {
                return await GetQuestionCorrectAnswersByContextId(contextId, historyId);
            }
            return JsonConvert.SerializeObject(null);
        }

        // Calling the chatbot service and receiving response
        private async Task<ChatbotResponse> ChatbotRequest(string query)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(query);

            if ( !response.IsSuccessStatusCode )
            {
                throw new Exception("Chatbot request failed!");
            }

            string content = await response.Content.ReadAsStringAsync();
            try {
                return JsonConvert.DeserializeObject<ChatbotResponse>(content);
            } catch (Exception e) {
                throw new Exception($"Could not de-serialize chatbot response: {e}");
            }
        }

        // Fetching new history id if necessary
        private async Task<int> GetHistoryId(bool getNewHistory, int initialHistoryId) {
            if ( !getNewHistory ) {
                return initialHistoryId;
            }
            string selectNewHistoryIdQuery = @"
                WITH history AS (
	                SELECT history_id + 1
	                FROM sessions
	                GROUP BY history_id
	                ORDER BY history_id DESC
	                LIMIT 1
                )
                SELECT
                CASE
	                WHEN NOT EXISTS (SELECT * FROM history) THEN 1
	                WHEN (SELECT * FROM history) IS NULL THEN 1
	                ELSE (SELECT * FROM history)
                END
                AS history_id
                FROM sections
                LIMIT 1
            ";
            int historyId = -1;
            await using (var cmd = new NpgsqlCommand(selectNewHistoryIdQuery, DB.connection)) {
                await using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        historyId = (int)reader[0];
                    }
                }
            }
            if ( historyId == -1 ) {
                throw new Exception("History Id couldn't be fetched.");
            }

            return historyId;
        }

        // Fetches the next section id and the given section data
        private async Task<Section> GetDataForResponseByRequestType(DataFetchingRequest dataFetchingRequest) {
            int nextSectionId;
            switch (dataFetchingRequest) {
                case DataFetchingRequest.NoType:
                    return null;
                case DataFetchingRequest.FetchNextSection:
                    nextSectionId = await FetchNextSectionId();
                    break;
                case DataFetchingRequest.FetchPreviousSection:
                    nextSectionId = await FetchPreviousSectionId();
                    break;
                case DataFetchingRequest.FetchSectionById:
                    nextSectionId = GetSectionIdFromQuestion();
                    break;
                case DataFetchingRequest.SearchForContent:
                    int sectionIdFound = Courses.SearchForSectionId(Question);
                    if (sectionIdFound == -1) {
                        botResponse.PickAnswer(1);
                        return null;
                    }
                    nextSectionId = sectionIdFound;
                    botResponse.PickAnswer(0);
                    break;
                default:
                    throw new Exception("DataFetchingRequest unknown");
            }

            return Courses.GetSectionById(nextSectionId);

        }

        // Getting section ID from question message
        private int GetSectionIdFromQuestion()
        {
            switch(_Type)
            {
                case RequestType.Section:
                    return Int32.Parse(Question);
                default:
                    throw new Exception("The request type has not implemented a way to get the section id from the request type");
            }
        }

        // Fetching next section id
        private async Task<int> FetchNextSectionId() {
            return await FetchSectionId(FetchingDirection.Forward, 1);
        }

        // Fetching previous section id
        private async Task<int> FetchPreviousSectionId() {
            return await FetchSectionId(FetchingDirection.Backward, 1);
        }

        // Fetching section id at N steps in either direction
        private async Task<int> FetchSectionId(FetchingDirection direction, int nSteps) {
            if (CourseId == -1 || HistoryId == -1) {
                throw new Exception("Course id og history id or both were not set. They're required for the method");
            }
            string directionSymbol = direction == FetchingDirection.Forward ? "+" : "-";
            string selectQuery = $@"
                WITH newSectionId AS (
	                SELECT ""order"" {directionSymbol} @nSteps FROM courses_sections
                    INNER JOIN sessions
                    ON courses_sections.course_id = sessions.course_id
                    AND courses_sections.section_id = sessions.section_id
                    WHERE history_id = @historyId
                    ORDER BY sessions.id DESC
                    LIMIT 1
                )
                SELECT sections.id
                FROM sections
                WHERE sections.id = (
                  SELECT section_id FROM courses_sections
                  WHERE course_id = @courseId AND ""order"" = (
                    SELECT
                    CASE
                      WHEN NOT EXISTS(SELECT* FROM newSectionId ) THEN 0
                      ELSE(SELECT * FROM newSectionId)
                    END
                  )
                );
            ";

            int sectionId = -1;
            await using (var cmd = new NpgsqlCommand(selectQuery, DB.connection)) {
                cmd.Parameters.AddWithValue("courseId", CourseId);
                cmd.Parameters.AddWithValue("historyId", HistoryId);
                cmd.Parameters.AddWithValue("nSteps", nSteps);
                await using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        sectionId = (int)reader[0];
                    }
                }
            }

            if (sectionId == -1) {
                throw new Exception("Section Id couldn't be fetched.");
            }

            return sectionId;
        }
    }
}

// TODO: Handle levels in quiz / description
