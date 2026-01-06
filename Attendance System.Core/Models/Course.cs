namespace AttendanceSystem.Core.Models
{
    public class Course
    {
        private int _id;
        private string _code = string.Empty;
        private string _title = string.Empty;
        private string _teacherEmail = string.Empty;

        public int Id { get => _id; set => _id = value; }
        public string Code { get => _code; set => _code = value.Trim(); }
        public string Title { get => _title; set => _title = value.Trim(); }
        public string TeacherEmail { get => _teacherEmail; set => _teacherEmail = value.Trim(); }
    }
}