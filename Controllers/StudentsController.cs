using AttendanceSystem.Business;
using AttendanceSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly UserService _userService;
        private readonly AppDbContext _context;

        public StudentController(UserService userService, AppDbContext context)
        {
            _userService = userService;
            _context = context; // ✅ Assign it
        }

        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");
            return View();
        }

        public IActionResult Attendance()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");

            // ✅ Get student email from JWT
            var studentEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(studentEmail))
                return RedirectToAction("Login", "Home");

            var attendanceRecords = _userService.GetAttendanceForStudent(studentEmail);

            if (attendanceRecords.Any())
            {
                var total = attendanceRecords.Count;
                var present = attendanceRecords.Count(a => a.Status == "Present");
                var late = attendanceRecords.Count(a => a.Status == "Late");
                var absent = attendanceRecords.Count(a => a.Status == "Absent");

                ViewBag.Total = total;
                ViewBag.Present = present;
                ViewBag.Late = late;
                ViewBag.Absent = absent;
                ViewBag.AttendancePercentage = Math.Round((double)(present + late * 0.5) / total * 100, 2);
            }

            return View(attendanceRecords);
        }

        // Show available courses
        public IActionResult RegisterForCourses()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");

            var studentEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(studentEmail))
                return RedirectToAction("Login", "Home");

            var allCourses = _userService.GetAllCourses();
            var enrolledCodes = _userService.GetEnrolledCourseCodes(studentEmail);
            var available = allCourses.Where(c => !enrolledCodes.Contains(c.Code)).ToList();

            ViewBag.AvailableCourses = available;
            return View();
        }

        // Enroll in selected courses
        [HttpPost]
        public IActionResult EnrollInCourse(List<string> courseCodes)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");

            var studentEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(studentEmail))
                return RedirectToAction("Login", "Home");

            if (courseCodes != null)
            {
                foreach (string code in courseCodes)
                {
                    if (!_userService.EnrollmentExists(studentEmail, code))
                    {
                        _userService.EnrollStudent(studentEmail, code);
                    }
                }
                TempData["Success"] = "Successfully enrolled!";
            }
            return RedirectToAction("RegisterForCourses");
        }
        public IActionResult MyTimetable()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");

            var studentEmail = User.Identity.Name;

            var timetables = _context.Timetables
                .FromSqlInterpolated($@"
            SELECT 
                t.Id, t.CourseCode, t.DayOfWeek,
                ISNULL(t.StartTime, '09:00:00') AS StartTime,
                ISNULL(t.EndTime, '10:00:00') AS EndTime,
                ISNULL(t.Room, '') AS Room
            FROM Timetables t
            INNER JOIN Enrollments e ON t.CourseCode = e.CourseCode
            WHERE e.StudentEmail = {studentEmail}
            ORDER BY t.DayOfWeek, t.StartTime")
                .ToList();

            return View(timetables);
        }
        public IActionResult MyReport()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Student"))
                return RedirectToAction("Login", "Home");

            var studentEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(studentEmail))
                return RedirectToAction("Login", "Home");

            var reports = _userService.GenerateAttendanceReport(studentEmail: studentEmail);
            return View(reports);
        }
    }
}