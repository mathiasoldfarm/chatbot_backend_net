using System;
using System.Data;
using System.Text.RegularExpressions;

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
        public bool usingQuiz {
            get; set;
        }
        public bool usingDescription {
            get; set;
        }

        public Section(DataRow row) {
            try {
                id = (int)row[0];
                name = (string)row[1];
                if (row[5] != DBNull.Value) {
                    usingQuiz = (bool)row[5];
                }
                if (row[6] != DBNull.Value) {
                    usingDescription = (bool)row[6];
                }
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have two arguments of type int string");
            }
        }


        public void AddDescription(Description _description) {
            description = _description;

        }

        public void AddQuiz(Quiz _quiz) {
            quiz = _quiz;
        }

        public void AddParent(int id) {
            parent = id;
        }
    }
}
