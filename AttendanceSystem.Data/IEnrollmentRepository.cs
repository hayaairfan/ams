using AttendanceSystem.Core.Models;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface IEnrollmentRepository
    {
        List<Enrollment> GetAllEnrollments();
        void AddEnrollment(Enrollment enrollment);
        bool EnrollmentExists(string studentEmail, string courseCode);
    }
}