using System;
using System.Data;
using System.Collections.Generic;

namespace chatbot_backend.Models {
    public class Description {
        public int id {
            get; set;
        }
        public List<DescriptionLevel> levels {
            get; set;
        }

        public Description(DataRow row) {
            try {
                id = (int)row[0];
                levels = new List<DescriptionLevel>();
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have ine argument of type int");
            }
        }

        public void AddDescriptionLevel(DescriptionLevel descriptionLevel) {
            levels.Add(descriptionLevel);
        }
    }
}
