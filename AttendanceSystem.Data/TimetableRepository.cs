// AttendanceSystem.Data/TimetableRepository.cs
using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceSystem.Data
{
    public class TimetableRepository : ITimetableRepository
    {
        private readonly AppDbContext _context;

        public TimetableRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Timetable> GetByCourse(string courseCode)
        {
            if (string.IsNullOrEmpty(courseCode))
            {
                return new List<Timetable>(); // Return empty list if no course code
            }

            return _context.Timetables
                .Where(t => t.CourseCode == courseCode) // Safe because we checked for null/empty
                .ToList();
        }

        public List<Timetable> GetByTeacher(string teacherEmail)
        {
            if (string.IsNullOrEmpty(teacherEmail))
            {
                return new List<Timetable>();
            }

            var courseCodes = _context.Courses
                .Where(c => c.TeacherEmail == teacherEmail)
                .Select(c => c.Code)
                .ToList();

            if (!courseCodes.Any())
            {
                return new List<Timetable>();
            }

            return _context.Timetables
                .Where(t => courseCodes.Contains(t.CourseCode))
                .ToList();
        }

        public void AddTimetable(Timetable timetable)
        {
            _context.Timetables.Add(timetable);
            _context.SaveChanges();
        }

        public bool TimetableExists(string courseCode, int dayOfWeek, TimeSpan startTime)
        {
            return _context.Timetables.Any(t =>
                t.CourseCode == courseCode &&
                t.DayOfWeek == dayOfWeek &&
                t.StartTime == startTime);
        }
    }
}