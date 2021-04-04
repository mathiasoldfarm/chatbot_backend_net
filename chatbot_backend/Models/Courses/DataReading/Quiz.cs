using System;
using System.Data;
using System.Collections.Generic;

namespace chatbot_backend.Models {
    public class Quiz {
        public int id {
            get; set;
        }
        public List<QuizLevel> levels {
            get; set;
        }

        public Quiz(DataRow row) {
            try {
                id = (int)row[0];
                levels = new List<QuizLevel>();
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have three arguments of type int string");
            }
        }

        public void AddQuizLevel(QuizLevel quizLevel) {
            levels.Add(quizLevel);
        }
    }
}
