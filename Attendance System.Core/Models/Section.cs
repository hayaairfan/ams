namespace AttendanceSystem.Core.Models
{
    public class Section
    {
        private int _id;
        private string _name = string.Empty;
        private string _session = string.Empty;

        public int Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value.Trim(); }
        public string Session { get => _session; set => _session = value.Trim(); }
    }
}