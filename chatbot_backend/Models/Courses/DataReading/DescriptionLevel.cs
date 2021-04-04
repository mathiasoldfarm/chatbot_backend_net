using System;
using System.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using Npgsql;

namespace chatbot_backend.Models {
    public class DescriptionLevel {
        public int id {
            get; set;
        }
        public string description {
            get; set;
        }
        public int level {
            get; set;
        }
        public DescriptionLevelCategory category {
            get; set;
        }

        public DescriptionLevel(DataRow row) {
            try {
                id = (int)row[0];
                description = (string)row[1];
                level = (int)row[2];
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have two arguments of type int, string, int");
            }
        }

        public void AddCategory(DescriptionLevelCategory _category) {
            category = _category;
        }
    }
}
