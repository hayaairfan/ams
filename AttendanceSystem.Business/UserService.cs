using AttendanceSystem.Core.Models;
using AttendanceSystem.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Data;




namespace AttendanceSystem.Business
{
    public class UserService
    {
        private readonly IUserRepository _userRepo;
        private readonly ISectionRepository _sectionRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ISectionAssignmentRepository _sectionAssignmentRepo;
        private readonly ITimetableRepository _timetableRepo;

        public UserService(
    IUserRepository userRepo,
    ICourseRepository courseRepo,
    IAttendanceRepository attendanceRepo,
    IEnrollmentRepository enrollmentRepo,
    ISectionRepository sectionRepo,
    ISectionAssignmentRepository sectionAssignmentRepo,
    ITimetableRepository timetableRepo) // ✅ Add this parameter
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _attendanceRepo = attendanceRepo;
            _enrollmentRepo = enrollmentRepo;
            _sectionRepo = sectionRepo;
            _sectionAssignmentRepo = sectionAssignmentRepo;
            _timetableRepo = timetableRepo; // ✅ Assign it
        }


        // === Password Security ===
        public string HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password), "Password cannot be null.");

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        public List<Timetable> GetAllTimetables()
        {
            return _timetableRepo.GetByCourse(null);
        }
        public bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;
            if (!Regex.IsMatch(password, @"[A-Z]")) return false;
            if (!Regex.IsMatch(password, @"[a-z]")) return false;
            if (!Regex.IsMatch(password, @"[0-9]")) return false;
            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]")) return false;
            return true;
        }

        // === User Management ===
        public (bool Success, string Message) RegisterUser(string email, string password, string role)
        {
            if (!IsStrongPassword(password))
                return (false, "Password must be 8+ chars with uppercase, lowercase, digit, and special char.");
            if (_userRepo.GetUserByEmail(email) != null)
                return (false, "Email already exists.");

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password), // ✅ This must match what you use in Login
                Role = role
            };
            _userRepo.AddUser(user);
            return (true, "User created successfully.");
        }
        

        public bool ValidatePassword(string email, string password)
        {
            var user = _userRepo.GetUserByEmail(email);
            return user != null && VerifyPassword(password, user.PasswordHash);
        }

        public bool UpdatePassword(string email, string newPassword)
        {
            var user = _userRepo.GetUserByEmail(email);
            if (user == null) return false;

            user.PasswordHash = HashPassword(newPassword);
            user.MustChangePassword = false; // ✅ Critical!

            _userRepo.UpdateUser(user); // Make sure this exists
            return true;
        }
        public User? Authenticate(string email, string password)
        {
            var user = _userRepo.GetUserByEmail(email);
            return (user != null && VerifyPassword(password, user.PasswordHash)) ? user : null;
        }

        // === Section Management ===
        public void AddSection(string name, string session)
        {
            _sectionRepo.AddSection(new Section { Name = name, Session = session });
        }

        public List<Section> GetAllSections() => _sectionRepo.GetAllSections();

        // === Course Management ===
        public void AddCourse(string code, string title, string teacherEmail)
        {
            _courseRepo.AddCourse(new Course { Code = code, Title = title, TeacherEmail = teacherEmail });
        }

        
        //public List<Timetable> GetTodaysClassesForTeacher(string teacherEmail)
        //{
        //    var today = (int)DateTime.Today.DayOfWeek;
        //    var courses = _courseRepo.GetCoursesByTeacher(teacherEmail);
        //    var courseCodes = courses.Select(c => c.Code).ToList();

        //    return _timetableRepo.GetByCourse(null) // Get all timetables
        //        .Where(t => courseCodes.Contains(t.CourseCode) && t.DayOfWeek == today)
        //        .ToList();
        //}

        // === Enrollment Management ===

        public List<Enrollment> GetAllEnrollments() => _enrollmentRepo.GetAllEnrollments();
        // === Attendance Management ===
        public List<string> GetStudentsInCourse(string courseCode)
        {
            return _attendanceRepo.GetStudentsInCourse(courseCode);
        }

        public List<Attendance> GetAttendanceByCourse(string courseCode, DateTime date)
        {
            return _attendanceRepo.GetAttendanceByCourse(courseCode, date);
        }
        public List<Attendance> GetAttendanceForStudent(string studentEmail)
        {
            return _attendanceRepo.GetAttendanceForStudent(studentEmail);
        }

        public Attendance? GetAttendance(string studentEmail, string courseCode, DateTime date)
        {
            return _attendanceRepo.GetAttendance(studentEmail, courseCode, date);
        }

        public void AddOrUpdateAttendance(Attendance attendance)
        {
            _attendanceRepo.AddOrUpdateAttendance(attendance);
        }

        //public List<Course> GetCoursesByTeacher(string teacherEmail)
        //{
        //    return _courseRepo.GetAllCourses()
        //        .Where(c => c.TeacherEmail == teacherEmail)
        //        .ToList();
        //}

        public List<Course> GetCoursesByTeacher(string teacherEmail)
        {
            return _courseRepo.GetCoursesByTeacher(teacherEmail);
        }

        //public List<Timetable> GetTodaysClassesForTeacher(string teacherEmail)
        //{
        //    var today = (int)DateTime.Today.DayOfWeek;
        //    var courses = _courseRepo.GetCoursesByTeacher(teacherEmail);
        //    var courseCodes = courses.Select(c => c.Code).ToList();

        //    return _timetableRepo.GetByCourse(null)
        //        .Where(t => courseCodes.Contains(t.CourseCode) && t.DayOfWeek == today)
        //        .ToList();
        //}
        // Add this method
        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        }),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes("ThisIsASecretKeyForAttendanceSystemProject2025!")),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        // Validation
        public bool SectionExists(string name, string session) => _sectionRepo.SectionExists(name, session);
        public bool CourseExists(string code) => _courseRepo.CourseExists(code);
        public bool UserExists(string email) => _userRepo.UserExists(email);
        public List<User> GetAllUsers()
        {
            return _userRepo.GetAllUsers();
        }

        // Get all teachers for dropdown
        public List<string> GetAllTeacherEmails()
        {
            return _userRepo.GetAllUsers()
                .Where(u => u.Role == "Teacher")
                .Select(u => u.Email)
                .ToList();
        }
        // Get all courses
        public List<Course> GetAllCourses() => _courseRepo.GetAllCourses();

        // Get courses a student is enrolled in
        public List<Enrollment> GetEnrolledCourses(string studentEmail)
        {
            return _enrollmentRepo.GetAllEnrollments()
                .Where(e => e.StudentEmail == studentEmail)
                .ToList();
        }

        // Check if enrollment exists
        public bool EnrollmentExists(string studentEmail, string courseCode)
        {
            return _enrollmentRepo.GetAllEnrollments()
                .Any(e => e.StudentEmail == studentEmail && e.CourseCode == courseCode);
        }
        // In UserService.cs
        //public List<Section> GetAllSections()
        //{
        //    return _sectionRepo.GetAll(); // Make sure you have ISectionRepository
        //}
        //public List<User> GetAllStudents()
        //{
        //    return _userRepo.GetAllUsers().Where(u => u.Role == "Student").ToList();
        //}

        //public bool IsStudentAssignedToSection(string studentEmail, string sectionName, string session)
        //{
        //    return _sectionAssignmentRepo.IsAssigned(studentEmail, sectionName, session);
        //}

        //public void AssignStudentToSection(string studentEmail, string sectionName, string session)
        //{
        //    _sectionAssignmentRepo.Assign(studentEmail, sectionName, session);
        //}
        //public List<Timetable> GetTodaysClassesForTeacher(string teacherEmail)
        //{
        //    var today = (int)DateTime.Today.DayOfWeek; // Sunday = 0
        //    var courses = _courseRepo.GetCoursesByTeacher(teacherEmail);
        //    var courseCodes = courses.Select(c => c.Code).ToList();

        //    return _timetableRepo.GetByCourse(null) // Get all
        //        .Where(t => courseCodes.Contains(t.CourseCode) && t.DayOfWeek == today)
        //        .ToList();
        //}
        //public List<Timetable> GetTodayTimetablesForTeacher(string teacherEmail)
        //{
        //    var today = (int)DateTime.Today.DayOfWeek;
        //    var courses = _courseRepo.GetCoursesByTeacher(teacherEmail);
        //    var courseCodes = courses.Select(c => c.Code).ToList();

        //    return _timetableRepo.GetByCourse(null)
        //        .Where(t => courseCodes.Contains(t.CourseCode) && t.DayOfWeek == today)
        //        .ToList();
        //}
        public List<Timetable> GetTodayTimetablesForTeacher(string teacherEmail)
        {
            var today = (int)DateTime.Today.DayOfWeek;
            Console.WriteLine($"[DEBUG] Today: {today} ({Enum.GetName(typeof(DayOfWeek), today)})");
            Console.WriteLine($"[DEBUG] Teacher Email: {teacherEmail}");

            var courses = _courseRepo.GetCoursesByTeacher(teacherEmail);
            Console.WriteLine($"[DEBUG] Courses taught by teacher: {courses.Count}");
            foreach (var c in courses)
            {
                Console.WriteLine($"[DEBUG] Course: {c.Code}, Teacher: {c.TeacherEmail}");
            }

            var courseCodes = courses.Select(c => c.Code).ToList();
            var allTimetables = _timetableRepo.GetByCourse(null);
            Console.WriteLine($"[DEBUG] Total timetables in system: {allTimetables.Count}");

            var filtered = allTimetables
                .Where(t => courseCodes.Contains(t.CourseCode) && t.DayOfWeek == today)
                .ToList();

            Console.WriteLine($"[DEBUG] Timetables for today: {filtered.Count}");
            foreach (var t in filtered)
            {
                Console.WriteLine($"[DEBUG] Matched: {t.CourseCode} on {t.DayOfWeek} from {t.StartTime} to {t.EndTime}");
            }

            return filtered;
        }
        public bool IsTimetableScheduledOn(string courseCode, DateTime date)
        {
            var day = (int)date.DayOfWeek;
            return _timetableRepo.GetByCourse(courseCode)
                .Any(t => t.DayOfWeek == day);
        }
        public bool IsCourseAssignedToTeacher(string courseCode, string teacherEmail)
        {
            var course = _courseRepo.GetByCode(courseCode);
            return course != null && course.TeacherEmail == teacherEmail;
        }
        public List<User> GetAllStudents()
        {
            return _userRepo.GetAllUsers().Where(u => u.Role == "Student").ToList();
        }
       

        public void AddAttendance(Attendance attendance)
        {
            _attendanceRepo.AddAttendance(attendance);
        }
        public User AuthenticateUser(string email, string password)
        {
            var user = _userRepo.GetUserByEmail(email);
            if (user == null) return null;

            if (VerifyPassword(password, user.PasswordHash)) // ✅ Uses your method
                return user;

            return null;
        }

        
        public bool IsStudentAssignedToSection(string studentEmail, string sectionName, string session)
        {
            return _sectionAssignmentRepo.IsAssigned(studentEmail, sectionName, session);
        }

        public void AssignStudentToSection(string studentEmail, string sectionName, string session)
        {
            _sectionAssignmentRepo.Assign(studentEmail, sectionName, session);
        }

        // Enroll student
        public void EnrollStudent(string studentEmail, string courseCode)
        {
            _enrollmentRepo.AddEnrollment(new Enrollment
            {
                StudentEmail = studentEmail,
                CourseCode = courseCode
            });
        }
        public List<string> GetEnrolledCourseCodes(string studentEmail)
        {
            return _enrollmentRepo.GetAllEnrollments()
                .Where(e => e.StudentEmail == studentEmail)
                .Select(e => e.CourseCode)
                .ToList();
        }
        // AttendanceSystem.Business/UserService.cs
        public List<AttendanceReport> GenerateAttendanceReport(
    string courseCode = null,
    string studentEmail = null,
    DateTime? startDate = null,
    DateTime? endDate = null)
        {
            IEnumerable<Attendance> query = _attendanceRepo.GetAll();

            if (!string.IsNullOrWhiteSpace(courseCode))
                query = query.Where(a => a.CourseCode == courseCode.Trim());

            if (!string.IsNullOrWhiteSpace(studentEmail))
                query = query.Where(a => a.StudentEmail == studentEmail.Trim());

            if (startDate.HasValue)
                query = query.Where(a => a.Date.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(a => a.Date.Date <= endDate.Value.Date);

            var grouped = query
                .GroupBy(a => new { a.StudentEmail, a.CourseCode })
                .Select(g => new AttendanceReport
                {
                    StudentEmail = g.Key.StudentEmail,
                    CourseCode = g.Key.CourseCode,
                    TotalClasses = g.Count(),
                    Present = g.Count(a => a.Status == "Present"),
                    Late = g.Count(a => a.Status == "Late"),
                    Absent = g.Count(a => a.Status == "Absent")
                })
                .ToList();

            foreach (var r in grouped)
            {
                r.Percentage = r.TotalClasses > 0
                    ? Math.Round((r.Present + r.Late * 0.5) / r.TotalClasses * 100, 2)
                    : 0;
            }

            return grouped;
        }
       
    }
}