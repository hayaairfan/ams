// AttendanceSystem.Data/ITimetableRepository.cs
using AttendanceSystem.Core.Models;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface ITimetableRepository
    {

        List<Timetable> GetByCourse(string courseCode);
        List<Timetable> GetByTeacher(string teacherEmail);
        void AddTimetable(Timetable timetable);
        bool TimetableExists(string courseCode, int dayOfWeek, TimeSpan startTime);
    }
}