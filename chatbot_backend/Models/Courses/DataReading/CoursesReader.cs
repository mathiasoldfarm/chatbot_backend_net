using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using System.Linq;

namespace chatbot_backend.Models {
    public class CoursesReader {
        public Dictionary<int, Answer> answers { get; private set; }
        public Dictionary<int, Question> questions { get; private set; }
        public Dictionary<int, QuizLevel> quizLevels { get; private set; }
        public Dictionary<int, Quiz> quizzes { get; private set; }
        public Dictionary<int, DescriptionLevelCategory> descriptionLevelCategories { get; private set; }
        public Dictionary<int, DescriptionLevel> descriptionLevels { get; private set; }
        public Dictionary<int, Description> descriptions { get; private set; }
        public Dictionary<int, Section> sections { get; private set; }
        public Dictionary<int, CourseCategory> courseCategories { get; private set; }
        public Dictionary<int, Course> courses { get; private set; }

        public CoursesReader() {
            answers = new Dictionary<int, Answer>();
            questions = new Dictionary<int, Question>();
            quizLevels = new Dictionary<int, QuizLevel>();
            quizzes = new Dictionary<int, Quiz>();
            descriptionLevelCategories = new Dictionary<int, DescriptionLevelCategory>();
            descriptionLevels = new Dictionary<int, DescriptionLevel>();
            descriptions = new Dictionary<int, Description>();
            sections = new Dictionary<int, Section>();
            courseCategories = new Dictionary<int, CourseCategory>();
            courses = new Dictionary<int, Course>();
        }

        private DataSet GetData(Dictionary<string, string> QueryData) {
            DataSet ds = new DataSet();

            foreach (KeyValuePair<string, string> QueryTable in QueryData) {
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(QueryTable.Key, DB.connection);
                ds.Tables.Add(QueryTable.Value);
                da.Fill(ds.Tables[QueryTable.Value]);
            }

            return ds;
        }

        private void GetAnswers(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                answers.Add(id, new Answer(row));
            }
        }

        private void GetQuestions(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                int correctId = (int)row[2];

                Question question = new Question(row);
                question.AddCorrect(answers[correctId]);

                questions.Add(id, question);
            }
        }

        private void GetQuizLevels(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                quizLevels.Add(id, new QuizLevel(row));
            }
        }

        private void GetQuizzes(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                quizzes.Add(id, new Quiz(row));
            }
        }

        private void GetDescriptionLevelCategories(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                descriptionLevelCategories.Add(id, new DescriptionLevelCategory(row));
            }
        }

        private void GetDescriptionLevels(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                object categoryId = row[3];

                DescriptionLevel descriptionLevel = new DescriptionLevel(row);

                if (categoryId != DBNull.Value) {
                    descriptionLevel.AddCategory(descriptionLevelCategories[(int)categoryId]);
                }

                descriptionLevels.Add(id, descriptionLevel);
            }
        }

        private void GetDescriptions(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                descriptions.Add(id, new Description(row));
            }
        }

        private void GetSections(DataTable table) {
            Dictionary<int, object> temporaryParentMapper = new Dictionary<int, object>();

            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                object parentId = row[4];
                Section section = new Section(row);

                object quizId = row[2];
                object descriptionId = row[3];
                if (quizId != DBNull.Value) {
                    section.AddQuiz(quizzes[(int)quizId]);
                }
                if (descriptionId != DBNull.Value) {
                    section.AddDescription(descriptions[(int)descriptionId]);
                }

                sections.Add(id, section);
                temporaryParentMapper.Add(id, parentId);
            }

            foreach (KeyValuePair<int, Section> section in sections) {
                object parentId = temporaryParentMapper[section.Key];
                if (parentId != DBNull.Value) {
                    section.Value.AddParent((int)parentId);
                }
            }
        }

        private void GetCourseCategories(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                courseCategories.Add(id, new CourseCategory(row));
            }
        }

        private void GetCourses(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int id = (int)row[0];
                string title = (string)row[1];
                string description = (string)row[2];
                int categoryId = (int)row[3];

                Course course = new Course(row);
                course.AddCategory(courseCategories[categoryId]);

                courses.Add(id, course);
            }
        }

        private void AddQuestionAnswerRelations(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int questionId = (int)row[0];
                int answerId = (int)row[1];
                int order = (int)row[2];
                questions[questionId].AddAnswer((order, answers[answerId]));
            }
            foreach (Question question in questions.Values) {
                question.SortAnswers();
            }
        }

        private void AddQuizLevelQuestionRelations(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int quizLevelId = (int)row[0];
                int questionId = (int)row[1];
                int order = (int)row[2];
                quizLevels[quizLevelId].AddQuestion((order, questions[questionId]));
            }
            foreach (QuizLevel quizLevel in quizLevels.Values) {
                quizLevel.SortQuestions();
            }
        }

        private void AddQuizQuizLevelRelations(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int quizId = (int)row[2];
                int quizLevelId = (int)row[0];
                quizzes[quizId].AddQuizLevel(quizLevels[quizLevelId]);
            }
        }

        private void AddDescriptionDescriptionLevelRelations(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int descriptionId = (int)row[4];
                int descriptionLevelId = (int)row[0];
                descriptions[descriptionId].AddDescriptionLevel(descriptionLevels[descriptionLevelId]);
            }
        }

        private void AddCourseSectionRelations(DataTable table) {
            foreach (DataRow row in table.Rows) {
                int courseId = (int)row[0];
                int sectionId = (int)row[1];
                int order = (int)row[2];
                courses[courseId].AddSection((order, sections[sectionId]));
            }
            foreach (Course course in courses.Values) {
                course.SortSections();
            }
        }

        private void ParseTag(string tag, ref string text) {
            string openTag = $"<{tag}>";
            string endTag = $"</{tag}>";
            if (text.Contains(openTag) && text.Contains(endTag)) {
                if (tag == "IMG") {
                    string images_url_base = "http://localhost:5001/images/";
                    text = text.Replace(openTag, $@"<img class=""description-{tag.ToLower()}"" src=""{images_url_base}");
                    text = text.Replace(endTag, @".png"" />");
                } else if (tag == "MATH") {
                    text = text.Replace(openTag, @"\(");
                    text = text.Replace(endTag, @"\)");
                } else if (tag == "MATHBIG") {
                    text = text.Replace(openTag, @"$$");
                    text = text.Replace(endTag, @"$$");
                } else {
                    text = text.Replace(openTag, $@"<div class=""description-{tag.ToLower()}"">");
                    text = text.Replace(endTag, "</div>");
                }
            }
        }

        private void ParseLines(ref string text, string[] tags) {
            string newText = "";
            string[] splitoptions = new string[tags.Length * 2 + 1];
            int counter = 0;
            foreach(string tag in tags) {
                string openTag = $"<{tag}>";
                string endTag = $"</{tag}>";

                splitoptions[counter] = openTag;
                counter += 1;
                splitoptions[counter] = endTag;
                counter += 1;
            }
            splitoptions[splitoptions.Length - 1] = "\n";

            string[] splittedByNewLines = text.Split(splitoptions, StringSplitOptions.RemoveEmptyEntries);
            if ( splittedByNewLines[splittedByNewLines.Length - 1] == "" ) {
                splittedByNewLines = splittedByNewLines.SkipLast(1).ToArray();
            }
            foreach (string substring in splittedByNewLines) {
                text = text.Replace(substring, "<p>" + substring + "</p>");
            }
        }

        private void Parse(ref string text) {
            string[] tags = new string[] { "DEFINITION", "TIP", "EKSEMPEL", "IMG", "MATH", "MATHBIG" };
            string[] leaveOut = new string[] { "MATH", "IMG", "MATHBIG" };
            ParseLines(ref text, tags.Where(x => !leaveOut.Contains(x) ).ToArray());
            foreach (string tag in tags) {
                ParseTag(tag, ref text);
            }
        }

        private void ParseSections() {
            foreach(Section section in sections.Values) {
                section.description = ParseDescription(section.description);
                section.quiz = ParseQuiz(section.quiz);
            }
        }

        private void ParseAnswers() {
            foreach (Answer answer in answers.Values) {
                string text = answer.answer;
                ParseTag("MATH", ref text);
                answer.answer = text;
            }
        }

        private Description ParseDescription(Description _description) {
            if (_description == null || _description.levels.Count == 0) {
                return _description;
            }
            string text = _description.levels[0].description;
            Parse(ref text);

            _description.levels[0].description = text;
            return _description;
        }

        private Quiz ParseQuiz(Quiz _quiz) {
            if (_quiz == null || _quiz.levels.Count == 0 || _quiz.levels[0].questions.Count == 0) {
                return _quiz;
            }
            string text = _quiz.levels[0].questions[0].question;
            Parse(ref text);

            _quiz.levels[0].questions[0].question = text;
            return _quiz;
        }

        private void ReadCourses() {
            string possibleAnswersTable = "possibleAnswers";
            string questionsTable = "questions";
            string quizLevelsTable = "quizLevels";
            string quizTable = "quiz";
            string descriptionLevelCategoriesTable = "descriptionLevelCategories";
            string descriptionLevelsTable = "descriptionLevels";
            string descriptionTable = "description";
            string sectionsTable = "sections";
            string courseCategoriesTable = "courseCategories";
            string coursesTable = "courses";

            string questionAnswerRelationsTable = "questionAnswerRelations";
            string quizLevelQuestionRelationsTable = "quizLevelQuestionRelations";
            string courseSectionRelations = "courseSectionRelations";


            Dictionary<string, string> QueryData = new Dictionary<string, string>();
            QueryData.Add("SELECT * FROM answers;", possibleAnswersTable);
            QueryData.Add("SELECT * FROM questions", questionsTable);
            QueryData.Add("SELECT * FROM quiz_levels", quizLevelsTable);
            QueryData.Add("SELECT * FROM quizzes", quizTable);
            QueryData.Add("SELECT * FROM description_categories", descriptionLevelCategoriesTable);
            QueryData.Add("SELECT * FROM description_levels", descriptionLevelsTable);
            QueryData.Add("SELECT * FROM descriptions", descriptionTable);
            QueryData.Add("SELECT * FROM sections", sectionsTable);
            QueryData.Add("SELECT * FROM categories", courseCategoriesTable);
            QueryData.Add("SELECT * FROM courses", coursesTable);

            QueryData.Add("SELECT * FROM questions_answers;", questionAnswerRelationsTable);
            QueryData.Add("SELECT * FROM quiz_levels_questions;", quizLevelQuestionRelationsTable);
            QueryData.Add("SELECT * FROM courses_sections", courseSectionRelations);

            DataSet DBData = GetData(QueryData);

            GetAnswers(DBData.Tables[possibleAnswersTable]);
            GetQuestions(DBData.Tables[questionsTable]);
            GetQuizLevels(DBData.Tables[quizLevelsTable]);
            GetQuizzes(DBData.Tables[quizTable]);
            GetDescriptionLevelCategories(DBData.Tables[descriptionLevelCategoriesTable]);
            GetDescriptionLevels(DBData.Tables[descriptionLevelsTable]);
            GetDescriptions(DBData.Tables[descriptionTable]);
            GetSections(DBData.Tables[sectionsTable]);
            GetCourseCategories(DBData.Tables[courseCategoriesTable]);
            GetCourses(DBData.Tables[coursesTable]);

            AddQuestionAnswerRelations(DBData.Tables[questionAnswerRelationsTable]);
            AddQuizLevelQuestionRelations(DBData.Tables[quizLevelQuestionRelationsTable]);
            AddQuizQuizLevelRelations(DBData.Tables[quizLevelsTable]);
            AddDescriptionDescriptionLevelRelations(DBData.Tables[descriptionLevelsTable]);
            AddCourseSectionRelations(DBData.Tables[courseSectionRelations]);
            ParseSections();
            ParseAnswers();
        }

        public CoursesReader run() {
            ReadCourses();

            return this;
        }
    }
}