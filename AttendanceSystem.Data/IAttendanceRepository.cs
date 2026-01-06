using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface IAttendanceRepository
    {
        Attendance? GetAttendance(string studentEmail, string courseCode, DateTime date);
        void AddOrUpdateAttendance(Attendance attendance);
        List<string> GetStudentsInCourse(string courseCode);
        List<Attendance> GetAttendanceForStudent(string studentEmail);
        List<Attendance> GetAttendanceByCourse(string courseCode, DateTime date);
        //List<Course> GetCoursesByTeacher(string teacherEmail);

        List<Attendance> GetAttendanceByFilters(
            string? courseCode = null,
            string? studentEmail = null,
            string? teacherEmail = null,
            DateTime? startDate = null,
            DateTime? endDate = null);// ✅ Added
        List<Attendance> GetAttendanceByFilters(string? courseCode = null, string? studentEmail = null, DateTime? startDate = null, DateTime? endDate = null);
        List<Attendance> GetAll();
        // In IAttendanceRepository.cs
        void AddAttendance(Attendance attendance);

        // In AttendanceRepository.cs
       
    }

}