using AttendanceSystem.Business;
using AttendanceSystem.Core.Models;
using AttendanceSystem.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AttendanceSystem.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserService _userService;
        private readonly ISectionRepository _sectionRepo; // ✅ Add this field
        private readonly ITimetableRepository _timetableRepo;

        public AdminController(
    UserService userService,
    ISectionRepository sectionRepo,
    ITimetableRepository timetableRepo) // ✅ Add this
        {
            _userService = userService;
            _sectionRepo = sectionRepo;
            _timetableRepo = timetableRepo; // ✅ Assign it
        }

        // === Dashboard ===
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");
            return View();
        }

        // === User Management ===
        public IActionResult ManageUsers()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(string email, string password, string role)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (_userService.UserExists(email))
            {
                ViewBag.Error = "User with this email already exists.";
                return View("ManageUsers");
            }

            var result = _userService.RegisterUser(email, password, role);
            if (result.Success)
            {
                ViewBag.Success = result.Message;
            }
            else
            {
                ViewBag.Error = result.Message;
            }
            return View("ManageUsers");
        }

        // === Section Management ===
        public IActionResult ManageSections()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult AddSection(string name, string session)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (_userService.SectionExists(name, session))
            {
                ViewBag.Error = "Section already exists in this session.";
                return View("ManageSections");
            }

            _userService.AddSection(name, session);
            ViewBag.Success = "Section added successfully!";
            return View("ManageSections");
        }

        // === Course Management ===
        public IActionResult ManageCourses()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            ViewBag.Teachers = _userService.GetAllTeacherEmails();
            return View();
        }

        [HttpPost]
        public IActionResult AddCourse(string code, string title, string teacherEmail)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (_userService.CourseExists(code))
            {
                ViewBag.Error = "Course code already exists.";
                ViewBag.Teachers = _userService.GetAllTeacherEmails();
                return View("ManageCourses");
            }

            var teachers = _userService.GetAllTeacherEmails();
            if (!teachers.Contains(teacherEmail))
            {
                ViewBag.Error = "Selected email is not a registered teacher.";
                ViewBag.Teachers = teachers;
                return View("ManageCourses");
            }

            _userService.AddCourse(code, title, teacherEmail);
            ViewBag.Success = "Course added successfully!";
            ViewBag.Teachers = teachers;
            return View("ManageCourses");
        }

        // === Enrollment Management ===
        public IActionResult EnrollStudents()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult EnrollStudent(string studentEmail, string courseCode)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (_userService.EnrollmentExists(studentEmail, courseCode))
            {
                ViewBag.Error = "Student is already enrolled in this course.";
                return View("EnrollStudents");
            }

            if (!_userService.UserExists(studentEmail))
            {
                ViewBag.Error = "Student email not found.";
                return View("EnrollStudents");
            }

            if (!_userService.CourseExists(courseCode))
            {
                ViewBag.Error = "Course code not found.";
                return View("EnrollStudents");
            }

            _userService.EnrollStudent(studentEmail, courseCode);
            ViewBag.Success = "Student enrolled successfully!";
            return View("EnrollStudents");
        }

        // === Assign Students to Sections ===
        public IActionResult AssignStudentsToSections()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
            ViewBag.Sections = _sectionRepo.GetAll(); // ✅ Use repo directly
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = "")
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var user = _userService.AuthenticateUser(email, password);
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // ✅ Create authentication cookie
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

            // ✅ Redirect to change password if required
            if (user.MustChangePassword)
            {
                return RedirectToAction("ChangePassword", "Account", new { force = true });
            }

            // Otherwise, go to dashboard
            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(user.Role == "Admin" ? "Index" : "Index", user.Role);
        }
        [HttpGet]
        public IActionResult ChangePassword(bool force = false)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            if (force)
                ViewBag.Message = "You must change your password on first login.";

            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword, bool force = false)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var email = User.Identity.Name;

            if (force && string.IsNullOrEmpty(currentPassword))
            {
                ViewBag.Error = "Current password is required.";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "New password and confirmation are required.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and confirmation do not match.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters long.";
                return View();
            }

            // Authenticate current password (if not first login, still require it)
            if (!force || !string.IsNullOrEmpty(currentPassword))
            {
                var isValid = _userService.ValidatePassword(email, currentPassword);
                if (!isValid)
                {
                    ViewBag.Error = "Current password is incorrect.";
                    return View();
                }
            }

            // ✅ Update password and clear MustChangePassword flag
            var success = _userService.UpdatePassword(email, newPassword);
            if (success)
            {
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index", User.IsInRole("Admin") ? "Admin" : User.IsInRole("Teacher") ? "Teacher" : "Student");
            }
            else
            {
                ViewBag.Error = "Failed to update password. Please try again.";
                return View();
            }
        }

        [HttpPost]
        public IActionResult AssignStudentsToSections(string studentEmail, string sectionName, string session)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(studentEmail) || string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(session))
            {
                ViewBag.Error = "All fields are required.";
            }
            else if (_userService.IsStudentAssignedToSection(studentEmail, sectionName, session))
            {
                ViewBag.Error = "Student is already assigned to this section.";
            }
            else
            {
                _userService.AssignStudentToSection(studentEmail, sectionName, session);
                ViewBag.Success = "Student assigned successfully!";
            }

            ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
            ViewBag.Sections = _sectionRepo.GetAll(); // ✅ Use repo directly
            return View();
        }
        public IActionResult ManageTimetable()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            ViewBag.Courses = _userService.GetAllCourses(); // Still use UserService for courses
            return View();
        }

        [HttpPost]
        public IActionResult AddTimetable(string courseCode, int dayOfWeek, string startTime, string endTime, string room)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            if (_timetableRepo.TimetableExists(courseCode, dayOfWeek, TimeSpan.Parse(startTime)))
            {
                ViewBag.Error = "Timetable slot already exists for this course.";
            }
            else
            {
                var timetable = new Timetable
                {
                    CourseCode = courseCode,
                    DayOfWeek = dayOfWeek,
                    StartTime = TimeSpan.Parse(startTime),
                    EndTime = TimeSpan.Parse(endTime),
                    Room = room
                };
                _timetableRepo.AddTimetable(timetable); // ✅ Use _timetableRepo directly
                ViewBag.Success = "Timetable added successfully!";
            }

            ViewBag.Courses = _userService.GetAllCourses();
            return View("ManageTimetable");
        }


        // === Reports ===
        public IActionResult Reports(string courseCode, string studentEmail, string startDate, string endDate)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

            var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

            ViewBag.Courses = _userService.GetAllCourses();
            ViewBag.Students = _userService.GetAllUsers().Where(u => u.Role == "Student").ToList();
            return View(reports);
        }

        public IActionResult ExportReports(string courseCode, string studentEmail, string startDate, string endDate)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
                return RedirectToAction("Login", "Home");

            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

            var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

            var csv = new StringBuilder();
            csv.AppendLine("Student,Course,Total,Present,Late,Absent,Percentage");
            foreach (var r in reports)
            {
                csv.AppendLine($"{r.StudentEmail},{r.CourseCode},{r.TotalClasses},{r.Present},{r.Late},{r.Absent},{r.Percentage}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "AttendanceReport.csv");
        }
    }
}
//using AttendanceSystem.Business;
//using AttendanceSystem.Core.Models;
//using Microsoft.AspNetCore.Mvc;
//using System.Text;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//namespace AttendanceSystem.Web.Controllers
//{
//    public class AdminController : Controller
//    {
//        private readonly UserService _userService;


//        public AdminController(UserService userService)
//        {
//            _userService = userService;
//        }

//        // === Dashboard ===
//        public IActionResult Index()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");
//            return View();
//        }

//        // === User Management ===
//        public IActionResult ManageUsers()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");
//            return View();
//        }

//        [HttpPost]
//        public IActionResult AddUser(string email, string password, string role)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            if (_userService.UserExists(email))
//            {
//                ViewBag.Error = "User with this email already exists.";
//                return View("ManageUsers");
//            }

//            var result = _userService.RegisterUser(email, password, role);
//            if (result.Success)
//            {
//                ViewBag.Success = result.Message;
//            }
//            else
//            {
//                ViewBag.Error = result.Message;
//            }
//            return View("ManageUsers");
//        }

//        // === Section Management ===
//        public IActionResult ManageSections()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");
//            return View();
//        }

//        [HttpPost]
//        public IActionResult AddSection(string name, string session)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            if (_userService.SectionExists(name, session))
//            {
//                ViewBag.Error = "Section already exists in this session.";
//                return View("ManageSections");
//            }

//            _userService.AddSection(name, session);
//            ViewBag.Success = "Section added successfully!";
//            return View("ManageSections");
//        }

//        // === Course Management ===
//        public IActionResult ManageCourses()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            ViewBag.Teachers = _userService.GetAllTeacherEmails();
//            return View();
//        }

//        [HttpPost]
//        public IActionResult AddCourse(string code, string title, string teacherEmail)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            if (_userService.CourseExists(code))
//            {
//                ViewBag.Error = "Course code already exists.";
//                ViewBag.Teachers = _userService.GetAllTeacherEmails();
//                return View("ManageCourses");
//            }

//            var teachers = _userService.GetAllTeacherEmails();
//            if (!teachers.Contains(teacherEmail))
//            {
//                ViewBag.Error = "Selected email is not a registered teacher.";
//                ViewBag.Teachers = teachers;
//                return View("ManageCourses");
//            }

//            _userService.AddCourse(code, title, teacherEmail);
//            ViewBag.Success = "Course added successfully!";
//            ViewBag.Teachers = teachers;
//            return View("ManageCourses");
//        }

//        // === Enrollment Management ===
//        public IActionResult EnrollStudents()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");
//            return View();
//        }

//        [HttpPost]
//        public IActionResult EnrollStudent(string studentEmail, string courseCode)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            if (_userService.EnrollmentExists(studentEmail, courseCode))
//            {
//                ViewBag.Error = "Student is already enrolled in this course.";
//                return View("EnrollStudents");
//            }

//            if (!_userService.UserExists(studentEmail))
//            {
//                ViewBag.Error = "Student email not found.";
//                return View("EnrollStudents");
//            }

//            if (!_userService.CourseExists(courseCode))
//            {
//                ViewBag.Error = "Course code not found.";
//                return View("EnrollStudents");
//            }

//            _userService.EnrollStudent(studentEmail, courseCode);
//            ViewBag.Success = "Student enrolled successfully!";
//            return View("EnrollStudents");
//        }

//        // GET: Admin/Reports
//        //public IActionResult Reports(string courseCode, string studentEmail, string startDate, string endDate)
//        //{
//        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//        //        return RedirectToAction("Login", "Home");

//        //    DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
//        //    DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

//        //    var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

//        //    // Provide all courses and students for filters
//        //    ViewBag.Courses = _userService.GetAllCourses().Select(c => c.Code).ToList();
//        //    ViewBag.Students = _userService.GetAllUsers()
//        //        .Where(u => u.Role == "Student")
//        //        .Select(u => u.Email)
//        //        .ToList();

//        //    return View(reports);
//        //}

//        //// Export to CSV (GET-based to avoid form issues)
//        //public IActionResult ExportReports(string courseCode, string studentEmail, string startDate, string endDate)
//        //{
//        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//        //        return RedirectToAction("Login", "Home");

//        //    DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
//        //    DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

//        //    var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

//        //    // Generate CSV
//        //    var csv = new StringBuilder();
//        //    csv.AppendLine("Student Email,Course Code,Total Classes,Present,Late,Absent,Attendance %");
//        //    foreach (var r in reports)
//        //    {
//        //        csv.AppendLine($"{r.StudentEmail},{r.CourseCode},{r.TotalClasses},{r.Present},{r.Late},{r.Absent},{r.Percentage}");
//        //    }

//        //    return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "AttendanceReport.csv");
//        //}
//        //public IActionResult Reports()
//        //{
//        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//        //        return RedirectToAction("Login", "Home");

//        //    var reports = _userService.GenerateAttendanceReport(); // No parameters
//        //    return View(reports);
//        //}
//        // GET: Admin/AssignStudentsToSections
//        public IActionResult AssignStudentsToSections()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
//            ViewBag.Sections = _userService.GetAllSections(); // Returns List<Section>

//            return View();
//        }
//        public IActionResult AssignStudentsToSections()
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
//            ViewBag.Sections = _sectionRepo.GetAll(); // ← Use repo directly or ensure UserService has access
//            return View();
//        }

//        //[HttpPost]
//        //public IActionResult AssignStudentsToSections(string studentEmail, string sectionName, string session)
//        //{
//        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//        //        return RedirectToAction("Login", "Home");

//        //    if (string.IsNullOrEmpty(studentEmail) || string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(session))
//        //    {
//        //        ViewBag.Error = "All fields are required.";
//        //    }
//        //    else if (_userService.IsStudentAssignedToSection(studentEmail, sectionName, session))
//        //    {
//        //        ViewBag.Error = "Student is already assigned to this section.";
//        //    }
//        //    else
//        //    {
//        //        _userService.AssignStudentToSection(studentEmail, sectionName, session);
//        //        ViewBag.Success = "Student assigned successfully!";
//        //    }

//        //    ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
//        //    ViewBag.Sections = _userService.GetAllSections();
//        //    return View();
//        //}
//        [HttpPost]
//        public IActionResult AssignStudentsToSections(string studentEmail, string sectionName, string session)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            if (string.IsNullOrEmpty(studentEmail) || string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(session))
//            {
//                ViewBag.Error = "All fields are required.";
//            }
//            else if (_userService.IsStudentAssignedToSection(studentEmail, sectionName, session))
//            {
//                ViewBag.Error = "Student is already assigned to this section.";
//            }
//            else
//            {
//                _userService.AssignStudentToSection(studentEmail, sectionName, session);
//                ViewBag.Success = "Student assigned successfully!";
//            }

//            ViewBag.Students = _userService.GetAllStudents().Select(s => s.Email).ToList();
//            ViewBag.Sections = _sectionRepo.GetAll(); // ✅ Use repo directly
//            return View();
//        }
//        public IActionResult Reports(string courseCode, string studentEmail, string startDate, string endDate)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
//            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

//            var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

//            // ✅ Provide all courses and students for filters (like Teacher)
//            ViewBag.Courses = _userService.GetAllCourses().Select(c => c.Code).ToList();
//            ViewBag.Students = _userService.GetAllUsers()
//                .Where(u => u.Role == "Student")
//                .Select(u => u.Email)
//                .ToList();

//            return View(reports);
//        }

//        // === CSV EXPORT (Original version, fixed for JWT) ===
//        public IActionResult ExportReports(string courseCode, string studentEmail, string startDate, string endDate)
//        {
//            // ✅ Use JWT-based auth (not Session)
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
//            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

//            var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

//            var csv = new StringBuilder();
//            csv.AppendLine("Student,Course,Total,Present,Late,Absent,Percentage");
//            foreach (var r in reports)
//            {
//                csv.AppendLine($"{r.StudentEmail},{r.CourseCode},{r.TotalClasses},{r.Present},{r.Late},{r.Absent},{r.Percentage}");
//            }

//            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "AttendanceReport.csv");
//        }

//        [HttpPost]
//        public IActionResult ExportPdf(string courseCode, string studentEmail, string startDate, string endDate)
//        {
//            if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//                return RedirectToAction("Login", "Home");

//            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
//            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

//            var reports = _userService.GenerateAttendanceReport(courseCode, studentEmail, start, end);

//            QuestPDF.Settings.License = LicenseType.Community;

//            var document = Document.Create(container =>
//            {
//                container.Page(page =>
//                {
//                    page.Size(PageSizes.A4);
//                    page.Margin(2, Unit.Centimetre);
//                    page.DefaultTextStyle(x => x.FontSize(10));

//                    page.Header()
//                        .Text("Attendance Report")
//                        .SemiBold()
//                        .FontSize(16)
//                        .FontColor(Colors.Blue.Medium);

//                    page.Content()
//                        .PaddingVertical(1, Unit.Centimetre)
//                        .Table(table =>
//                        {
//                            // Columns
//                            table.ColumnsDefinition(columns =>
//                            {
//                                columns.RelativeColumn(); // Student
//                                columns.RelativeColumn(); // Course
//                                columns.RelativeColumn(); // Total
//                                columns.RelativeColumn(); // Present
//                                columns.RelativeColumn(); // Late
//                                columns.RelativeColumn(); // Absent
//                                columns.RelativeColumn(); // %
//                            });

//                            // Header style
//                            var headerStyle = TextStyle.Default.SemiBold().FontSize(9);

//                            // Header row
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Student").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Course").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Total").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Present").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Late").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Absent").Style(headerStyle);
//                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5, Unit.Point).AlignCenter().Text("Attendance %").Style(headerStyle);

//                            // Data rows
//                            foreach (var r in reports)
//                            {
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.StudentEmail);
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.CourseCode);
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.TotalClasses.ToString());
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.Present.ToString());
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.Late.ToString());
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text(r.Absent.ToString());
//                                table.Cell().Padding(5, Unit.Point).AlignCenter().Text($"{r.Percentage}%");
//                            }
//                        });

//                    page.Footer()
//                        .AlignCenter()
//                        .Text(text =>
//                        {
//                            text.CurrentPageNumber();
//                            text.Span(" / ");
//                            text.TotalPages();
//                        });
//                });
//            });

//            var pdfBytes = document.GeneratePdf();
//            return File(pdfBytes, "application/pdf", "AttendanceReport.pdf");
//        }

//        // Helper for table style
//        //static void CellStyle(IContainer container)
//        //{
//        //    container.Background(Colors.Grey.Lighten3).Padding(5).ExtendHorizontal();
//        //}
//        // GET: Admin/Reports
//        //public IActionResult Reports()
//        //{
//        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
//        //        return RedirectToAction("Login", "Home");

//        //    ViewBag.Courses = _userService.GetAllCourses();
//        //    ViewBag.Students = _userService.GetAllUsers().Where(u => u.Role == "Student").ToList();
//        //    ViewBag.Reports = new List<AttendanceReport>();
//        //    return View();
//        //}



//    }
//}