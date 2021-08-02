using System;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using chatbot_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace chatbot_backend.Controllers.Views {
    [ApiController]
    [Authorize]
    [Route("views/opret-kursus")]
    public class CreateCourse : ControllerBase {
        public class CreateCourseData {
            public List<CreateCourseSection> sections {
                get; set;
            }
            public string title {
                get; set;
            }
        }

        public class CreateCourseAnswer {
            public string answer {
                get; set;
            }
            public int answerId {
                get; set;
            }
        }

        public class CreateCourseQuestion {
            public string question {
                get; set;
            }
            public int questionId {
                get; set;
            }
            public List<CreateCourseAnswer> answers {
                get {
                    return answersData.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
                }
                set {
                }
            }

            public int selected {
                get; set;
            }

            private List<(int, CreateCourseAnswer)> answersData {
                get; set;
            }

            public void AddAnswer(int order, string answerText, int answerId) {
                if (answersData == null) {
                    answersData = new List<(int, CreateCourseAnswer)>();
                }
                CreateCourseAnswer answer = new CreateCourseAnswer();
                answer.answer = answerText;
                answer.answerId = answerId;
                answersData.Add((order, answer));
            }
        }

        public class CreateCourseSection {
            public bool information {
                get; set;
            }
            public bool quiz {
                get; set;
            }
            public string informationText {
                get; set;
            }
            public int informationTextId {
                get; set;
            }
            public List<CreateCourseQuestion> questions {
                get; set;
            }
            public int questionIndex {
                get; set;
            }
            public int sectionId {
                get; set;
            }
        }
        public class CreateCourseDBData {
            public string title {
                get; set;
            }
            public int section_id {
                get; set;
            }
            public int sectionOrder {
                get; set;
            }
            public int quizId {
                get; set;
            }
            public int descriptionId {
                get; set;
            }
            public string question {
                get; set;
            }
            public int question_id {
                get; set;
            }
            public int correct {
                get; set;
            }
            public int question_order {
                get; set;
            }
            public string answer {
                get; set;
            }
            public int answer_id {
                get; set;
            }
            public int answer_order {
                get; set;
            }
            public string description {
                get; set;
            }
            public int description_level_id {
                get; set;
            }
            public bool using_quiz {
                get; set;
            }
            public bool using_description {
                get; set;
            }

        }

        [HttpGet]
        public async Task<IActionResult> Get() {
            try {
                string email = string.Empty;
                if (HttpContext.User.Identity is ClaimsIdentity identity) {
                    email = identity.FindFirst(ClaimTypes.Email).Value;
                }

                string fetchQuery = @"
                SELECT 
                title,
                sections.id as section_id,
                courses_sections.order as section_order,
                sections.quiz_id,
                sections.description_id,
                question,
                questions.id as question_id,
                correct,
                quiz_levels_questions.order as question_order,
                answer,
                answers.id as answer_id,
                questions_answers.order as answer_order,
                description_levels.description,
                description_levels.id as description_level_id,
                sections.using_quiz,
                sections.using_description
                FROM courses
                LEFT OUTER JOIN courses_sections ON course_id = courses.id
                LEFT OUTER JOIN sections ON section_id = sections.id
                LEFT OUTER JOIN quizzes ON quiz_id = quizzes.id
                LEFT OUTER JOIN quiz_levels ON quiz_levels.quiz_id = quizzes.id
                LEFT OUTER JOIN quiz_levels_questions ON quiz_levels.id = quiz_levels_questions.quiz_level_id
                LEFT OUTER JOIN questions ON quiz_levels_questions.question_id = questions.id
                LEFT OUTER JOIN questions_answers ON questions_answers.question_id = questions.id
                LEFT OUTER JOIN answers ON answers.id = questions_answers.possible_answer_id
                LEFT OUTER JOIN descriptions ON description_id = descriptions.id
                LEFT OUTER JOIN description_levels ON descriptions.id = description_levels.description_id
                WHERE courses.draft AND courses.created_by = 
                (SELECT id FROM users WHERE email = @email);";

                bool found = false;
                CreateCourseData data = new CreateCourseData();

                List<CreateCourseDBData> currentDraftCourseData = new List<CreateCourseDBData>();
                await using (var cmd = new NpgsqlCommand(fetchQuery, DB.connection)) {
                    cmd.Parameters.AddWithValue("email", email);
                    await using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            found = true;

                            CreateCourseDBData createCourseDBData = new CreateCourseDBData();
                            createCourseDBData.title = TryUnpackDBStringValue(0, reader);
                            createCourseDBData.section_id = TryUnpackDBIntValue(1, reader);
                            createCourseDBData.sectionOrder = TryUnpackDBIntValue(2, reader);
                            createCourseDBData.quizId = TryUnpackDBIntValue(3, reader);
                            createCourseDBData.descriptionId = TryUnpackDBIntValue(4, reader);
                            createCourseDBData.question = TryUnpackDBStringValue(5, reader);
                            createCourseDBData.question_id = TryUnpackDBIntValue(6, reader);
                            createCourseDBData.correct = TryUnpackDBIntValue(7, reader);
                            createCourseDBData.question_order = TryUnpackDBIntValue(8, reader);
                            createCourseDBData.answer = TryUnpackDBStringValue(9, reader);
                            createCourseDBData.answer_id = TryUnpackDBIntValue(10, reader);
                            createCourseDBData.answer_order = TryUnpackDBIntValue(11, reader);
                            createCourseDBData.description = TryUnpackDBStringValue(12, reader);
                            createCourseDBData.description_level_id = TryUnpackDBIntValue(13, reader);
                            createCourseDBData.using_quiz = TryUnpackDBBoolValue(14, reader);
                            createCourseDBData.using_description = TryUnpackDBBoolValue(15, reader);

                            currentDraftCourseData.Add(createCourseDBData);
                        }
                    }
                }

                if (!found) {
                    string insertQuery = @"INSERT INTO courses(title, description, created_by, draft) VALUES(@title, @description, (SELECT id FROM users WHERE email = @email), true)";
                    NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, DB.connection);
                    cmd.Parameters.AddWithValue("email", email);
                    cmd.Parameters.AddWithValue("title", "");
                    cmd.Parameters.AddWithValue("description", "");
                    cmd.ExecuteNonQuery();
                    data.title = "";
                } else {
                    Dictionary<int, CreateCourseQuestion> questions = new Dictionary<int, CreateCourseQuestion>();
                    foreach (CreateCourseDBData createCourseDBData in currentDraftCourseData) {
                        if (createCourseDBData.question_id != -1) {
                            if (questions.ContainsKey(createCourseDBData.question_id)) {
                                questions[createCourseDBData.question_id].AddAnswer(createCourseDBData.answer_order, createCourseDBData.answer, createCourseDBData.answer_id);
                            } else {
                                CreateCourseQuestion createCourseQuestion = new CreateCourseQuestion();
                                createCourseQuestion.answers = new List<CreateCourseAnswer>();
                                createCourseQuestion.question = createCourseDBData.question;
                                createCourseQuestion.selected = createCourseDBData.correct;
                                createCourseQuestion.questionId = createCourseDBData.question_id;
                                createCourseQuestion.AddAnswer(createCourseDBData.answer_order, createCourseDBData.answer, createCourseDBData.answer_id);
                                questions[createCourseDBData.question_id] = createCourseQuestion;
                            }
                        }
                    }

                    Dictionary<int, List<(int, CreateCourseQuestion)>> questionSections = new Dictionary<int, List<(int, CreateCourseQuestion)>>();
                    Dictionary<int, HashSet<int>> addedQuestionsToEachSection = new Dictionary<int, HashSet<int>>();
                    foreach (CreateCourseDBData createCourseDBData in currentDraftCourseData) {
                        if (createCourseDBData.quizId != -1) {
                            if (questionSections.ContainsKey(createCourseDBData.section_id)) {
                                if (!addedQuestionsToEachSection[createCourseDBData.section_id].Contains(createCourseDBData.question_id)) {
                                    questionSections[createCourseDBData.section_id].Add((createCourseDBData.question_order, questions[createCourseDBData.question_id]));
                                    addedQuestionsToEachSection[createCourseDBData.section_id].Add(createCourseDBData.question_id);
                                }
                            } else {
                                List<(int, CreateCourseQuestion)> listOfQuestions = new List<(int, CreateCourseQuestion)>();
                                if (createCourseDBData.question_id != -1) {
                                    listOfQuestions.Add((createCourseDBData.question_order, questions[createCourseDBData.question_id]));
                                }
                                questionSections[createCourseDBData.section_id] = listOfQuestions;

                                addedQuestionsToEachSection[createCourseDBData.section_id] = new HashSet<int>();
                                addedQuestionsToEachSection[createCourseDBData.section_id].Add(createCourseDBData.question_id);
                            }
                        }
                    }

                    List<(int, CreateCourseSection)> sections = new List<(int, CreateCourseSection)>();
                    HashSet<int> addedSections = new HashSet<int>();
                    foreach (CreateCourseDBData createCourseDBData in currentDraftCourseData) {
                        if ( !addedSections.Contains(createCourseDBData.section_id) ) {
                            CreateCourseSection createCourseSection = new CreateCourseSection();
                            if (createCourseDBData.using_quiz) {
                                createCourseSection.sectionId = createCourseDBData.section_id;
                                createCourseSection.quiz = true;
                                createCourseSection.information = false;
                                createCourseSection.informationText = createCourseDBData.description;
                                createCourseSection.questionIndex = 0;
                                createCourseSection.informationTextId = createCourseDBData.description_level_id;
                            } else {
                                createCourseSection.sectionId = createCourseDBData.section_id;
                                createCourseSection.quiz = false;
                                createCourseSection.information = true;
                                createCourseSection.informationText = createCourseDBData.description;
                                createCourseSection.informationTextId = createCourseDBData.description_level_id;
                            }
                            if (questionSections.ContainsKey(createCourseDBData.section_id)) {
                                createCourseSection.questions = questionSections[createCourseDBData.section_id].OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
                                createCourseSection.questionIndex = 0;
                            } else {
                                createCourseSection.questions = new List<CreateCourseQuestion>();
                                createCourseSection.questionIndex = -1;
                            }
                            addedSections.Add(createCourseDBData.section_id);
                            sections.Add((createCourseDBData.sectionOrder, createCourseSection));
                        }
                    }

                    data.title = currentDraftCourseData[0].title;
                    data.sections = sections.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
                }

                return Ok(data);
            } catch(Exception e) {
                return BadRequest(e.ToString());
            }
        }

        private string TryUnpackDBStringValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return "";
            }
            return (string)reader[index];
        }

        private int TryUnpackDBIntValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return -1;
            }
            return (int)reader[index];
        }

        private bool TryUnpackDBBoolValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return false;
            }
            return (bool)reader[index];
        }
    }
}