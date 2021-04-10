using System;
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
    }
}
