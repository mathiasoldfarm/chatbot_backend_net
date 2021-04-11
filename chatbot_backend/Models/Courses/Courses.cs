using System.Linq;
using System.Collections.Generic;

namespace chatbot_backend.Models {
    public static class Courses {
        public static CoursesReader CourseData;

        public static void FetchCoursesData() {
            CoursesReader reader = new CoursesReader();
            CourseData = reader.run();
        }

        public static Section GetSectionById(int sectionId) {
            return CourseData.sections[sectionId];
        }

        // Searching for content
        /*
        Simple search for now. Can be extended.
        Returns the Section containing a description with the most occurences of the search phrase.
        If none does, then nothing is returned.

        TODO: Handle multiple levels
        */

        public static int SearchForSectionId(string toSearchFor)
        {
            int NumberOfOccurences(Section section)
            {
                Description description = CourseData.descriptions[section.description.id];
                string text = description.levels.Where(l => l.level == 1).First().description.ToLower();
                return (text.Length - text.Replace(toSearchFor.ToLower(), "").Length) / toSearchFor.Length;
            }

            bool SectionIsDescriptionAndHasLevel1(Section section)
            {
                return section.description != null && section.description.levels.Any(l => l.level == 1);
            }

            IEnumerable<Section> sections = CourseData.sections.Values
                // FInds all sections that has a description and that description has a level of 1
                .Where(s => SectionIsDescriptionAndHasLevel1(s))
                // Orders all sections by their descriptions level 1's number of occurences of the search string
                .OrderByDescending(s => NumberOfOccurences(s));
            if (sections.Count() > 0 && NumberOfOccurences(sections.First()) != 0) {
                return sections.First().id;
            }
            return -1;
        }
    }
}
