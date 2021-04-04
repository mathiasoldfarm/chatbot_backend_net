using System;
using System.Data;

namespace chatbot_backend.Models {
    public class DescriptionLevelCategory {
        public int id {
            get; set;
        }
        public string category {
            get; set;
        }

        public DescriptionLevelCategory(DataRow row) {
            try {
                id = (int)row[0];
                category = (string)row[1];
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have three arguments of type int, string string");
            }
        }
    }
}
