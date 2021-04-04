using System;
using System.Collections.Generic;

namespace chatbot_backend.Models {
    public static class Courses {
        public static List<Course> courses;

        public static void FetchCoursesData() {
            Reader reader = new Reader();
            courses = reader.run();
        }
    }
}
