using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace chatbot_backend.Models {
    public class Course {
        public int id {
            get; set;
        }
        public string title {
            get; set;
        }
        public string description {
            get; set;
        }
        public CourseCategory category {
            get; set;
        }
        private List<(int, Section)> sectionsData {
            get; set;
        }

        public List<Section> sections {
            get {
                return sectionsData.Select(x => x.Item2).ToList();
            }
            set
            {
                if (value != null) {
                    sectionsData = new List<(int, Section)>();
                    for (int i = 0; i < value.Count; i++) {
                        sectionsData.Add((i, value[i]));
                    }
                }
            }
        }   

        public Course(DataRow row) {
            try {
                id = (int)row[0];
                title = (string)row[1];
                description = (string)row[2];
                sectionsData = new List<(int, Section)>();
            }
            catch {
                throw new Exception("Constructor argument DataRow was expected to have three arguments of type int, string, string");
            }
        }

        public void AddCategory(CourseCategory _category) {
            category = _category;
        }

        public void AddSection((int, Section) items) {
            sectionsData.Add(items);
        }

        public void SortSections() {
            sectionsData.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
    }
}
