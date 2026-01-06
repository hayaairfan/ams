using AttendanceSystem.Core.Models;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface ISectionRepository
    {
        List<Section> GetAllSections();
        void AddSection(Section section);
        bool SectionExists(string name, string session);
        List<Section> GetAll();
    }
    
    }