using System;
using System.Data;

namespace chatbot_backend.Models {
    public class Answer
    {
        public int id {
            get; set;
        }
        public string answer {
            get; set;
        }
        public string explanation {
            get; set;
        }

        public Answer(DataRow row) {
            try {
                id = (int)row[0];
                answer = (string)row[1];
                explanation = (string)row[2];
            } catch {
                throw new Exception("Constructor argument DataRow was expected to have three arguments of type int, string string");
            }
        }
    }
}
