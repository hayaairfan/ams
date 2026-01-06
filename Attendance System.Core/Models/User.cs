namespace AttendanceSystem.Core.Models
{
    public class User
    {
        private int _id;
        private string _email = string.Empty;
        private string _passwordHash = string.Empty;
        private string _role = "Student"; // Admin, Teacher, Student

        public bool MustChangePassword { get; set; } = true;
        public int Id { get => _id; set => _id = value; }
        public string Email { get => _email; set => _email = value.Trim(); }
        public string PasswordHash { get => _passwordHash; set => _passwordHash = value; }
        public string Role { get => _role; set => _role = value.Trim(); }
    }
}