namespace AttendanceSystem.Core.Models
{
    public class AttendanceReport
    {
        public string StudentEmail { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public double Percentage { get; set; }
    }
}