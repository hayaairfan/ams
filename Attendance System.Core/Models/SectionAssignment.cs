namespace AttendanceSystem.Core.Models
{
    public class SectionAssignment
    {
        public int Id { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string Session { get; set; } = string.Empty;
    }
}