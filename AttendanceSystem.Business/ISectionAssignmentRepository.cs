using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Data
{
    public class SectionAssignmentRepository : ISectionAssignmentRepository
    {
        private readonly AppDbContext _context;

        public SectionAssignmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public bool IsAssigned(string studentEmail, string sectionName, string session)
        {
            return _context.SectionAssignments.Any(a =>
                a.StudentEmail == studentEmail &&
                a.SectionName == sectionName &&
                a.Session == session);
        }

        public void Assign(string studentEmail, string sectionName, string session)
        {
            var assignment = new SectionAssignment
            {
                StudentEmail = studentEmail,
                SectionName = sectionName,
                Session = session
            };
            _context.SectionAssignments.Add(assignment);
            _context.SaveChanges();
        }
    }
}