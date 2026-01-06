using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceSystem.Data
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly AppDbContext _context;

        public EnrollmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Enrollment> GetAllEnrollments()
        {
            return _context.Enrollments.ToList();
        }
        public void AddEnrollment(Enrollment enrollment)
        {
            _context.Enrollments.Add(enrollment);
            _context.SaveChanges();
        }
        public bool EnrollmentExists(string studentEmail, string courseCode)
        {
            return _context.Enrollments.Any(e => e.StudentEmail == studentEmail && e.CourseCode == courseCode);
        }
    }
}