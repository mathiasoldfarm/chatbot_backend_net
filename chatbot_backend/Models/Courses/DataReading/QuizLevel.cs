using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace chatbot_backend.Models {
    public class QuizLevel
    {
        public int id { get; set; }
        public int level {
            get; set;
        }
        private  List<(int, Question)> questionsData {
            get; set;
        }

        public List<Question> questions {
            get {
                return questionsData.Select(x => x.Item2).ToList();
            }
            set
            {
                if (value != null)
                {
                    questionsData = new List<(int, Question)>();
                    for (int i = 0; i < value.Count; i++)
                    {
                        questionsData.Add((i, value[i]));
                    }
                }
            }
        }

        public QuizLevel(DataRow row) {
            try {
                id = (int)row[0];
                level = (int)row[1];
                questionsData = new List<(int, Question)>();
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have three arguments of type int, int string");
            }
        }

        public void AddQuestion((int, Question) items) {
            questionsData.Add(items);
        }

        public void SortQuestions() {
            questionsData.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}
