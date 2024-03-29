﻿using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using chatbot_backend.Controllers.Utils;

namespace chatbot_backend.Controllers.Bot {
    [ApiController]
    [Route("bot/getanswer")]
    public class GetAnswer : ControllerBase {
        public class Data {
            public int courseId { get; set; }
            public int contextId { get; set; }
            public int initialHistoryId { get; set; }
            public int type { get; set; }
            public string question { get; set; }
            public string requestData {
                get; set;
            }
            public int sectionId {
                get; set;
            }
        }

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
            SearchForContent,
            TakeInput
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
        private class ChatbotResponse {
            public string Answer { get; private set; }
            public List<string> NextPossibleAnswers { get; private set; }
            public DataFetchingRequest Type { get; private set; }
            public bool GetNewHistory { get; private set; }
            public int NextContextId {
                get; private set;
            }
            public bool SetDone {
                get; private set;
            }

            public void PickAnswer(int i) {
                string[] possibleAnswers = Answer.Split("|");
                if (possibleAnswers.Length - 1 < i) {
                    throw new Exception($"You can't pick an answer at index {i} of {possibleAnswers.Length} answers given by that chatbot service ");
                }

                Answer = possibleAnswers[i];
            }

            public ChatbotResponse(string answer, List<string> nextPossibleAnswers, int type, bool getNewHistory, int nextContextId, bool setDone) {
                Answer = answer;
                NextPossibleAnswers = nextPossibleAnswers;
                Type = (DataFetchingRequest)type;
                GetNewHistory = getNewHistory;
                NextContextId = nextContextId;
                SetDone = setDone;
            }
        }
        private class ClientData {
            public Section Section {
                get; private set;
            }
            public int CourseId {
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
            public int SectionDone {
                get; private set;
            }


            public ClientData(Section section, int courseId, ChatbotResponse botResponse, int historyId, int sectionDone) {
                Section = section;
                ContextId = botResponse.NextContextId;
                Answer = botResponse.Answer;
                HistoryId = historyId;
                if (!section.usingDescription && !section.usingQuiz) {
                    if ( section.quiz == null ) {
                        NextPossibleAnswers = botResponse.NextPossibleAnswers;
                    }
                } else {
                    if ( section.usingDescription ) {
                        NextPossibleAnswers = botResponse.NextPossibleAnswers;
                    }
                }
                CourseId = courseId;
                SectionDone = sectionDone;
            }
        }

        private class HistoryInstance {
            public int sessionId {
                get; set;
            }
            public int sectionId {
                get; set;
            }
            public int quizId {
                get; set;
            }
            public int descriptionId {
                get; set;
            }

            public HistoryInstance(int sessionId, int sectionId, int quizId, int descriptionId) {
                this.sessionId = sessionId;
                this.sectionId = sectionId;
                this.quizId = quizId;
                this.descriptionId = descriptionId;
            }
        }

        private class History {
            public List<HistoryInstance> history;

            public History() {
                history = new List<HistoryInstance>();
            }

            public void Add(int sessionId, int sectionId, int quizId, int descriptionId) {
                history.Add(new HistoryInstance(sessionId, sectionId, quizId, descriptionId));
            }
        }

        int CourseId { get; set; } = -1;
        int HistoryId { get; set; } = -1;
        string Question { get; set; }
        RequestType _Type { get; set; }
        ChatbotResponse botResponse { get; set; }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Data data) {
            string userEmail = "";
            if (HttpContext.User.Identity is ClaimsIdentity identity) {
                userEmail = (string)identity.FindFirst(ClaimTypes.Email).Value;
            }

            try {
                CourseId = data.courseId;
                _Type = (RequestType)data.type;
                Question = data.question;

                string botQuery = await GenerateBotQuery(data.contextId, data.initialHistoryId);
                botResponse = await ChatbotRequest(botQuery);

                ClientData response = await HandleChatbotResponse(userEmail, data);
                return Ok(response);
            } catch (Exception e) {
                return BadRequest(e.Message);
            }
        }

        private async Task<ClientData> HandleChatbotResponse(string userEmail, Data data) {
            HistoryId = await GetHistoryId(botResponse.GetNewHistory, data.initialHistoryId);
            Section section = await GetDataForResponseByRequestType(botResponse.Type);

            int SectionDone = -1;
            if ( botResponse.Type != DataFetchingRequest.TakeInput ) {
                SectionDone = await SetUserSectionDone(data.contextId, HistoryId, userEmail, botResponse.SetDone);
                Session session = new Session(CourseId, section, Question, botResponse.Answer, botResponse.NextContextId, HistoryId);
                session.insert();
            }

            InsertFeedback(data);

            return new ClientData(section, data.courseId, botResponse, HistoryId, SectionDone);
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
            url += $"&section={history}";

            return url;
        }

        // Fetching sessions id's by history id
        private async Task<string> GetHistory(int historyId)
        {
            string fetchQuery = @"
            SELECT sessions.id, section_id, sections.quiz_id, sections.description_id
            FROM sessions
            INNER JOIN sections ON sections.id = sessions.section_id
            WHERE history_id = @historyId
            ORDER BY sessions.id;";

            History history = new History();
            await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection))
            {
                cmd.Parameters.AddWithValue("historyId", historyId);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        history.Add(
                            Unpacker.UnpackDBIntValue(0, reader),
                            Unpacker.UnpackDBIntValue(1, reader),
                            Unpacker.UnpackDBIntValue(2, reader),
                            Unpacker.UnpackDBIntValue(3, reader)
                        );
                    }
                }
            }

            try {
                return JsonConvert.SerializeObject(history.history);
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

            ChatbotResponse botResponse;
            try {
                botResponse = JsonConvert.DeserializeObject<ChatbotResponse>(content);
             } catch (Exception e) {
                throw new Exception($"Could not de-serialize chatbot response: {e}");
            }

            if (botResponse.Type == DataFetchingRequest.NoType) {
                throw new Exception("Bot couldn't process request");
            }
            return botResponse;
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
                case DataFetchingRequest.TakeInput:
                    return new Section(true);
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
                      WHEN NOT EXISTS(SELECT* FROM newSectionId) THEN 0
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

        private async Task<int> SetUserSectionDone(int contextId, int historyId, string userEmail, bool SetDone) {
            int previousSection = -1;
            if ( SetDone ) {
                string selectQuery = "SELECT section_id FROM sessions WHERE history_id = @historyId and context_id = @contextId";
                await using (var cmd = new NpgsqlCommand(selectQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("historyId", historyId);
                    cmd.Parameters.AddWithValue("contextId", contextId);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            previousSection = (int)reader[0];
                        }
                    }
                }

                try {
                    string insertQuery = @"INSERT INTO users_sections_done(""user"", ""section"") VALUES((SELECT id FROM users WHERE email = @userEmail),@sectionId)";

                    NpgsqlCommand insertCmd = new NpgsqlCommand(insertQuery, DB.connection);
                    insertCmd.Parameters.AddWithValue("userEmail", userEmail);
                    insertCmd.Parameters.AddWithValue("sectionId", previousSection);
                    insertCmd.ExecuteNonQuery();

                } catch (PostgresException e) {
                    if ( e.Code != "23505" ) {
                        throw new Exception("Could not set users section done");
                    }
                }
            }
            

            return previousSection;
        }

        private void InsertFeedback(Data data) {
            if (data.question.ToLower() == "send feedback" && data.requestData.Length > 0) { // TODO make solution not harcoded
                try {
                    string insertQuery = @"INSERT INTO feedback(feedback, section) VALUES(@feedback ,@sectionId)";

                    NpgsqlCommand insertCmd = new NpgsqlCommand(insertQuery, DB.connection);
                    insertCmd.Parameters.AddWithValue("feedback", data.requestData);
                    insertCmd.Parameters.AddWithValue("sectionId", data.sectionId);
                    insertCmd.ExecuteNonQuery();
                } catch(PostgresException e) {
                    throw new Exception($"Failed inserting feedback: {e.Detail}");
                }
            }
        }
    }
}

// TODO: Handle levels in quiz / description
