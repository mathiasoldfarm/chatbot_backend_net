using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace chatbot_backend.Models {
    public class Question
    {
        public int id {
            get; set;
        }
        public string question {
            get; set;
        }
        public Answer correct {
            get; set;
        }
        private List<(int, Answer)> possibleAnswersData {
            get; set;
        }

        public List<Answer> possibleAnswers {
            get {
                return possibleAnswersData.Select(x => x.Item2).ToList();
            }
            set
            {
                if (value != null)
                {
                    possibleAnswersData = new List<(int, Answer)>();
                    for (int i = 1; i <= value.Count; i++)
                    {
                        possibleAnswersData.Add((i, value[i-1]));
                    }
                }
            }
        }

        public Question(DataRow row)
        {
            try {
                id = (int)row[0];
                question = (string)row[1];
                possibleAnswersData = new List<(int, Answer)>();
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have two arguments of type int, string");
            }
        }

        public void AddCorrect(Answer _correct) {
            correct = _correct;
        }

        public void AddAnswer((int, Answer) items) {
            possibleAnswersData.Add(items);
        }

        public void SortAnswers() {
            possibleAnswersData.Sort((x, y) => y.Item1.CompareTo(x.Item1));
        }

    }
}
