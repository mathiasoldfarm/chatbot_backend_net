using System;
using System.Data;

namespace chatbot_backend.Models {
    public class Section {
        public int id {
            get; set;
        }
        public string name {
            get; set;
        }
        public Quiz quiz {
            get; set;
        }
        public Description description {
            get; set;
        }
        public int parent {
            get; set;
        }

        public Section(DataRow row) {
            try {
                id = (int)row[0];
                name = (string)row[1];
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have two arguments of type int string");
            }
        }


        public void AddDescription(Description _description) {
            if ( quiz != null ) {
                throw new Exception("Description cannot be set if quiz is not null");
            } else {
                description = _description;
            }
        }

        public void AddQuiz(Quiz _quiz) {
            if (description != null) {
                throw new Exception("Quiz cannot be set if description is not null");
            }
            else {
                quiz = _quiz;
            }
        }

        public void AddParent(int id) {
            parent = id;
        }
    }
}
