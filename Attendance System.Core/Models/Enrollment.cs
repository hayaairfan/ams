namespace AttendanceSystem.Core.Models
{
    public class Enrollment
    {
        private int _id;
        private string _studentEmail = string.Empty;
        private string _courseCode = string.Empty;

        public int Id { get => _id; set => _id = value; }
        public string StudentEmail { get => _studentEmail; set => _studentEmail = value.Trim(); }
        public string CourseCode { get => _courseCode; set => _courseCode = value.Trim(); }
    }
}