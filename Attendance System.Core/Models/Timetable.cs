// AttendanceSystem.Core/Models/Timetable.cs
namespace AttendanceSystem.Core.Models
{
    public class Timetable
    {
        public int Id { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }       // 0 = Sunday, 1 = Monday, etc.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Room { get; set; } = string.Empty;
    }
}