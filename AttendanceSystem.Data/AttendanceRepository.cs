using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceSystem.Data
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly AppDbContext _context;

        public AttendanceRepository(AppDbContext context)
        {
            _context = context;
        }

        public Attendance? GetAttendance(string studentEmail, string courseCode, DateTime date)
        {
            return _context.Attendances
                .FirstOrDefault(a => a.StudentEmail == studentEmail
                                  && a.CourseCode == courseCode
                                  && a.Date.Date == date.Date);
        }

        public void AddOrUpdateAttendance(Attendance newRecord)
        {
            var existing = GetAttendance(newRecord.StudentEmail, newRecord.CourseCode, newRecord.Date);

            if (existing != null)
            {
                existing.Status = newRecord.Status;
            }
            else
            {
                _context.Attendances.Add(newRecord);
            }
            _context.SaveChanges();
        }

        public List<string> GetStudentsInCourse(string courseCode)
        {
            return _context.Enrollments
                .Where(e => e.CourseCode == courseCode)
                .Select(e => e.StudentEmail)
                .ToList();
        }
        public void AddAttendance(Attendance attendance)
        {
            // If already exists, update; else add
            var existing = _context.Attendances
                .FirstOrDefault(a => a.StudentEmail == attendance.StudentEmail
                                  && a.CourseCode == attendance.CourseCode
                                  && a.Date.Date == attendance.Date.Date);

            if (existing != null)
            {
                existing.Status = attendance.Status;
            }
            else
            {
                _context.Attendances.Add(attendance);
            }
            _context.SaveChanges();
        }

        public List<Attendance> GetAttendanceForStudent(string studentEmail)
        {
            return _context.Attendances
                .Where(a => a.StudentEmail == studentEmail)
                .ToList(); // ✅ Add .ToList()
        }

        // ✅ Added this
        public List<Attendance> GetAttendanceByCourse(string courseCode, DateTime date)
        {
            return _context.Attendances
                .Where(a => a.CourseCode == courseCode && a.Date == date)
                .ToList(); // ✅ Add .ToList()
        }

        public List<Attendance> GetAttendanceByFilters(string? courseCode = null, string? studentEmail = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Attendances.AsQueryable();

            if (!string.IsNullOrEmpty(courseCode))
                query = query.Where(a => a.CourseCode == courseCode);
            if (!string.IsNullOrEmpty(studentEmail))
                query = query.Where(a => a.StudentEmail == studentEmail);
            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value.Date);

            return query.ToList();
        }
        public List<Attendance> GetAll()
        {
            return _context.Attendances.ToList();
        }
        public List<Attendance> GetAttendanceByFilters(
    string? courseCode = null,
    string? studentEmail = null,
    string? teacherEmail = null,
    DateTime? startDate = null,
    DateTime? endDate = null)
        {
            var query = _context.Attendances.AsQueryable();

            // Filter by teacher: get courses assigned to teacher, then filter attendance
            if (!string.IsNullOrEmpty(teacherEmail))
            {
                var courseCodes = _context.Courses
                    .Where(c => c.TeacherEmail == teacherEmail)
                    .Select(c => c.Code)
                    .ToList();
                query = query.Where(a => courseCodes.Contains(a.CourseCode));
            }

            if (!string.IsNullOrEmpty(courseCode))
                query = query.Where(a => a.CourseCode == courseCode);
            if (!string.IsNullOrEmpty(studentEmail))
                query = query.Where(a => a.StudentEmail == studentEmail);
            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value.Date);

            return query.ToList();
        }
    }
}