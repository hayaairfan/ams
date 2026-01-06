using AttendanceSystem.Business;
using AttendanceSystem.Core.Models;
using AttendanceSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AttendanceSystem.Web.Controllers
{
    public class TeacherController : Controller
    {
        private readonly UserService _userService;
        private readonly AppDbContext _context;

        public TeacherController(UserService userService, AppDbContext context)
        {
            _userService = userService;
            _context = context; // ✅ Inject DbContext
        }



        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
                return RedirectToAction("Login", "Home");

            // ✅ Get teacher's email from JWT
            var teacherEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(teacherEmail))
                return RedirectToAction("Login", "Home");

            // ✅ Get assigned courses
            var courses = _userService.GetCoursesByTeacher(teacherEmail);
            ViewBag.Courses = courses;

            return View();
        }

        //public IActionResult MarkAttendance(string courseCode, string date = null)
        //{
        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
        //        return RedirectToAction("Login", "Home");

        //    var teacherEmail = User.Identity.Name;
        //    var courses = _userService.GetCoursesByTeacher(teacherEmail!);

        //    if (!courses.Any(c => c.Code == courseCode))
        //    {
        //        TempData["Error"] = "You are not assigned to this course.";
        //        return RedirectToAction("Index");
        //    }

        //    var attendanceDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
        //    var students = _userService.GetStudentsInCourse(courseCode); // ✅ Must return students
        //    var existingAttendance = _userService.GetAttendanceByCourse(courseCode, attendanceDate);

        //    ViewBag.CourseCode = courseCode;
        //    ViewBag.Date = attendanceDate.ToString("yyyy-MM-dd");
        //    ViewBag.Students = students; // ✅ Pass students to view
        //    ViewBag.ExistingAttendance = existingAttendance.ToDictionary(a => a.StudentEmail, a => a.Status);

        //    return View();
        //}
        public IActionResult MyTimetable()
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
                return RedirectToAction("Login", "Home");

            var teacherEmail = User.Identity.Name;

            var timetables = _context.Timetables
                .FromSqlInterpolated($@"
            SELECT 
                t.Id, t.CourseCode, t.DayOfWeek,
                ISNULL(t.StartTime, '09:00:00') AS StartTime,
                ISNULL(t.EndTime, '10:00:00') AS EndTime,
                ISNULL(t.Room, '') AS Room
            FROM Timetables t
            INNER JOIN Courses c ON t.CourseCode = c.Code
            WHERE c.TeacherEmail = {teacherEmail}
            ORDER BY t.DayOfWeek, t.StartTime")
                .ToList();

            return View(timetables);
        }
        public IActionResult MarkAttendance(string courseCode = null)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
                return RedirectToAction("Login", "Home");

            var teacherEmail = User.Identity.Name;
            var today = (int)DateTime.Today.DayOfWeek;

            // ✅ RAW SQL WITH FromSqlInterpolated — SIMPLE AND SAFE
            var timetables = _context.Timetables
    .FromSqlInterpolated($@"
SELECT t.Id, t.CourseCode, t.DayOfWeek,
       ISNULL(t.StartTime, '00:00:00') AS StartTime,
       ISNULL(t.EndTime, '00:00:00') AS EndTime,
       ISNULL(t.Room, '') AS Room
FROM Timetables t
INNER JOIN Courses c ON t.CourseCode = c.Code
WHERE c.TeacherEmail = {teacherEmail}
  AND t.DayOfWeek = {today}")
    .ToList();

            ViewBag.Timetables = timetables;

            if (!string.IsNullOrEmpty(courseCode))
            {
                // ✅ Validate: is this course assigned to teacher?
                var isAssigned = _context.Courses.Any(c => c.Code == courseCode && c.TeacherEmail == teacherEmail);
                if (!isAssigned)
                {
                    TempData["Error"] = "You are not assigned to this course.";
                    return RedirectToAction("MarkAttendance");
                }

                ViewBag.CourseCode = courseCode;
                ViewBag.Date = DateTime.Today.ToString("yyyy-MM-dd");

                // ✅ Get students enrolled in this course
                var students = _context.Enrollments
                    .Where(e => e.CourseCode == courseCode)
                    .Select(e => e.StudentEmail)
                    .Distinct()
                    .ToList();

                ViewBag.Students = students;

                // ✅ Get existing attendance for today
                var existingAttendance = _context.Attendances
                    .Where(a => a.CourseCode == courseCode && a.Date.Date == DateTime.Today.Date)
                    .ToDictionary(a => a.StudentEmail, a => a.Status);

                ViewBag.ExistingAttendance = existingAttendance;
            }

            return View();
        }
        //[HttpPost]
        //public IActionResult SaveAttendance(string courseCode, DateTime date, List<string> studentEmails, List<string> statusList)
        //{
        //    if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
        //        return RedirectToAction("Login", "Home");

        //    var teacherEmail = User.Identity.Name;
        //    if (string.IsNullOrEmpty(teacherEmail))
        //        return RedirectToAction("Login", "Home");

        //    // Optional: Verify teacher is assigned to course
        //    var courses = _userService.GetCoursesByTeacher(teacherEmail);
        //    if (!courses.Any(c => c.Code == courseCode))
        //    {
        //        TempData["Error"] = "You are not authorized to mark attendance for this course.";
        //        return RedirectToAction("Index");
        //    }

        //    for (int i = 0; i < studentEmails.Count; i++)
        //    {
        //        var attendance = new Attendance
        //        {
        //            StudentEmail = studentEmails[i],
        //            CourseCode = courseCode,
        //            Date = date,
        //            Status = statusList[i]
        //        };
        //        _userService.AddOrUpdateAttendance(attendance);
        //    }

        //    TempData["Success"] = "Attendance saved successfully!";
        //    return RedirectToAction("MarkAttendance", new { courseCode = courseCode, date = date.ToString("yyyy-MM-dd") });
        //}
        [HttpPost]
        public IActionResult SaveAttendance(string courseCode, string date, List<string> studentEmails, List<string> statusList)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
                return RedirectToAction("Login", "Home");

            var teacherEmail = User.Identity.Name;

            // ✅ Validate required fields
            if (string.IsNullOrWhiteSpace(courseCode))
            {
                TempData["Error"] = "Course code is required.";
                return RedirectToAction("MarkAttendance");
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                TempData["Error"] = "Date is required.";
                return RedirectToAction("MarkAttendance");
            }

            if (studentEmails == null || statusList == null || studentEmails.Count != statusList.Count)
            {
                TempData["Error"] = "Student or attendance data is missing.";
                return RedirectToAction("MarkAttendance");
            }

            // ✅ Validate date format
            if (!DateTime.TryParse(date, out DateTime attendanceDate))
            {
                TempData["Error"] = "Invalid date format.";
                return RedirectToAction("MarkAttendance");
            }

            // ✅ Validate: Is this course assigned to the teacher?
            var isAssigned = _context.Courses.Any(c => c.Code == courseCode && c.TeacherEmail == teacherEmail);
            if (!isAssigned)
            {
                TempData["Error"] = "You are not authorized to mark attendance for this course.";
                return RedirectToAction("MarkAttendance");
            }

            // ✅ Validate: Is today a scheduled day for this course?
            var isScheduled = _context.Timetables
                .Any(t => t.CourseCode == courseCode && t.DayOfWeek == (int)attendanceDate.DayOfWeek);

            if (!isScheduled)
            {
                TempData["Error"] = "Attendance can only be marked on scheduled class days.";
                return RedirectToAction("MarkAttendance");
            }

            // ✅ Save attendance records
            try
            {
                for (int i = 0; i < studentEmails.Count; i++)
                {
                    var email = studentEmails[i]?.Trim();
                    var status = statusList[i]?.Trim() ?? "Absent";

                    if (string.IsNullOrEmpty(email))
                        continue;

                    var attendance = new Attendance
                    {
                        StudentEmail = email,
                        CourseCode = courseCode,
                        Date = attendanceDate,
                        Status = status
                    };

                    // ✅ Check if already exists → update, else add
                    var existing = _context.Attendances
                        .FirstOrDefault(a => a.StudentEmail == email
                                          && a.CourseCode == courseCode
                                          && a.Date.Date == attendanceDate.Date);

                    if (existing != null)
                    {
                        existing.Status = status;
                    }
                    else
                    {
                        _context.Attendances.Add(attendance);
                    }
                }

                _context.SaveChanges(); // ✅ Save all changes at once

                TempData["Success"] = "Attendance saved successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving attendance. Please try again.";
                return RedirectToAction("MarkAttendance");
            }

            return RedirectToAction("MarkAttendance", new { courseCode = courseCode });
        }

        public IActionResult MyReports(string courseCode, string startDate, string endDate)
        {
            if (!User.Identity.IsAuthenticated || !User.IsInRole("Teacher"))
                return RedirectToAction("Login", "Home");

            var teacherEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(teacherEmail))
                return RedirectToAction("Login", "Home");

            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

            var reports = _userService.GenerateAttendanceReport(
                courseCode: courseCode,
                startDate: start,
                endDate: end);

            // Filter: only courses assigned to this teacher
            var myCourses = _userService.GetCoursesByTeacher(teacherEmail).Select(c => c.Code).ToList();
            reports = reports.Where(r => myCourses.Contains(r.CourseCode)).ToList();

            ViewBag.Courses = myCourses;
            return View(reports);
        }
    }
}