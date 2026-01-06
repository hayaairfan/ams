using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceSystem.Data
{
    public class SectionRepository : ISectionRepository
    {
        private readonly AppDbContext _context;
        public SectionRepository(AppDbContext context) => _context = context;

        public List<Section> GetAllSections() => _context.Sections.ToList();
        public void AddSection(Section section)
        {
            _context.Sections.Add(section);
            var entries = _context.ChangeTracker.Entries().ToList(); // Debug
            var rowsAffected = _context.SaveChanges();
            
            // Force error if nothing saved
            if (rowsAffected == 0)
                throw new Exception("No rows were saved to Sections table.");
        }
        public List<Section> GetAll()
        {
            return _context.Sections?.ToList() ?? new List<Section>(); // ✅ Never returns null
        }
        public bool SectionExists(string name, string session)
        {
            return _context.Sections.Any(s => s.Name == name && s.Session == session);
        }
    }
}