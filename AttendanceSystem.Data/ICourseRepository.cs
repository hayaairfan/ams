using AttendanceSystem.Core.Models;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface ICourseRepository
    {
        List<Course> GetAllCourses();
        void AddCourse(Course course);
        bool CourseExists(string code);
        List<Course> GetCoursesByTeacher(string teacherEmail);
        Course GetByCode(string code);
        
    }

}