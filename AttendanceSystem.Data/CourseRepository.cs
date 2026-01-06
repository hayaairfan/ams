using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceSystem.Data
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        public CourseRepository(AppDbContext context) => _context = context;

        public List<Course> GetAllCourses() => _context.Courses.ToList();
        public void AddCourse(Course course) { _context.Courses.Add(course); _context.SaveChanges(); }
        public bool CourseExists(string code)
        {
            return _context.Courses.Any(c => c.Code == code);
        }
        //public List<Course> GetCoursesByTeacher(string teacherEmail)
        //{
        //    return _context.Courses.Where(c => c.TeacherEmail == teacherEmail).ToList();
        //}
        public Course GetByCode(string code)
        {
            return _context.Courses.FirstOrDefault(c => c.Code == code);
        }

        public List<Course> GetCoursesByTeacher(string teacherEmail)
        {
            return _context.Courses.Where(c => c.TeacherEmail == teacherEmail).ToList();
        }
    }
}