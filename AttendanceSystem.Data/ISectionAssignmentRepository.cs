namespace AttendanceSystem.Data
{
    public interface ISectionAssignmentRepository
    {
        bool IsAssigned(string studentEmail, string sectionName, string session);
        void Assign(string studentEmail, string sectionName, string session);
    }
}