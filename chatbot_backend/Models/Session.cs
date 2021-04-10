using System;
using Npgsql;

namespace chatbot_backend.Models {
    public class Session {
        public int Id {
            get; private set;
        }
        public int CourseId {
            get; private set;
        }
        public int SectionId {
            get; private set;
        }
        public int Level {
            get; private set;
        } = 1;
        public string Question {
            get; private set;
        }
        public string Answer {
            get; private set;
        }
        public DateTime Time {
            get; private set;
        }
        public int ContextId {
            get; private set;
        }
        public int HistoryId {
            get; private set;
        }

        public Session(int courseId, Section section, string question, string answer, int contextId, int historyId) {
            // TODO: Handle level in session

            CourseId = courseId;
            SectionId = section.id;
            Question = question;
            Answer = answer;
            Time = DateTime.Now;
            ContextId = contextId;
            HistoryId = historyId;
        }

        public void insert() {
            NpgsqlCommand query = new NpgsqlCommand("INSERT INTO sessions(course_id, section_id, level, question, answer, time, context_id, history_id) VALUES(@courseId, @sectionId, @level, @question, @answer, @time, @contextId, @historyId)", DB.connection);
            query.Parameters.AddWithValue("courseId", CourseId);
            query.Parameters.AddWithValue("sectionId", SectionId);
            query.Parameters.AddWithValue("level", Level);
            query.Parameters.AddWithValue("question", Question);
            query.Parameters.AddWithValue("answer", Answer);
            query.Parameters.AddWithValue("time", Time);
            query.Parameters.AddWithValue("contextId", ContextId);
            query.Parameters.AddWithValue("historyId", HistoryId);
            query.ExecuteNonQuery();
        }
    }
}
