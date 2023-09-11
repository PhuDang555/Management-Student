using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCS2010PPTG4.Data;

namespace TCS2010PPTG4.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int departmentId = -1)
        {
            var studentIds = await _context.UserRoles.Where(u => u.RoleId == "Student")
                                                     .Select(u => u.UserId)
                                                     .ToListAsync();
            var coorIds = await _context.UserRoles.Where(u => u.RoleId == "Coordinator")
                                                     .Select(u => u.UserId)
                                                     .ToListAsync();
            var students = await _context.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();
            var coors = await _context.Users.Where(u => coorIds.Contains(u.Id)).ToListAsync();
            //var studentsOfDepartment = students.Where(s => s.DepartmentId == departmentId).ToList();
            var studentOfIT = students.Where(s => s.DepartmentId == 1).ToList();
            var studentOfBA = students.Where(s => s.DepartmentId == 2).ToList();
            var studentOfDesign = students.Where(s => s.DepartmentId == 3).ToList();

            var coorOfIT = coors.Where(s => s.DepartmentId == 1).ToList();
            var coorOfBA = coors.Where(s => s.DepartmentId == 2).ToList();
            var coorOfDesign = coors.Where(s => s.DepartmentId == 3).ToList();

            ViewData["TotalStudentOfIT"] = studentOfIT.Count();
            ViewData["TotalStudentOfBA"] = studentOfBA.Count();
            ViewData["TotalStudentOfDesign"] = studentOfDesign.Count();

            ViewData["TotalCoorOfIT"] = coorOfIT.Count();
            ViewData["TotalCoorOfBA"] = coorOfBA.Count();
            ViewData["TotalCoorOfDesign"] = coorOfDesign.Count();
            //ViewData["TotalStudentOfDept"] = studentsOfDepartment.Count();
            ViewData["TotalStudent"] = studentIds.Count();
            ViewData["TotalCoor"] = coorIds.Count();
            ViewData["TotalDepartment"] = await _context.Department.CountAsync();
            return View();
        }
    }
}
