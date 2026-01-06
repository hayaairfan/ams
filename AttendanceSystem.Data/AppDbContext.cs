using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<SectionAssignment> SectionAssignments { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
    }
}