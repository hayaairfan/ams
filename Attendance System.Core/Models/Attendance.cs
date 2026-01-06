namespace AttendanceSystem.Core.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        // ✅ Updated: Use string for status (Present/Absent/Late)
        public string Status { get; set; } = "Absent"; // Default: Absent
    }
}