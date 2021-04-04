using System;
using System.Data;

namespace chatbot_backend.Models {
    public class CourseCategory {
        public int id {
            get; set;
        }
        public string category {
            get; set;
        }

        public string colorClass
        {
            get; set;
        }

        public CourseCategory(DataRow row) {
            try {
                id = (int)row[0];
                category = (string)row[1];
                colorClass = (string)row[2];
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have two arguments of type int, string");
            }
        }
    }
}
