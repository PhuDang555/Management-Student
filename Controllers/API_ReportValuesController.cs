using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TCS2010PPTG4.Controllers
{
    [Route("api/report")]
    [ApiController]
    public class API_ReportValuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public API_ReportValuesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("manager_contribution")]
        [Produces("application/json")]
        public async Task<IActionResult> Department_Contribution()
        {
            try
            {
                var currenYear = DateTime.Now.Year;
                var topicIds = await _context.Topic.Where(s => s.CreationDate.Year == currenYear)
                                                  .Select(s =>s.Id)
                                                  .ToListAsync();
                var contributions = await _context.Contribution.Where(c => topicIds.Contains(c.TopicId))
                                                    .ToListAsync();
                List<API_Department_Contribution> statistics = new List<API_Department_Contribution>();
                foreach (var department in await _context.Department.ToListAsync())
                {
                    var contributorIds = await _context.Users.Where(u => u.DepartmentId == department.Id)
                                                             .Select(u => u.Id)   
                                                             .ToListAsync();
                    var totalContribution = contributions.Where(c => contributorIds.Contains(c.ContributorId))
                                                        .Count();
                    var temp = new API_Department_Contribution()
                    {
                        DepartmentName = department.Name,
                        TotalContribution = totalContribution
                    };
                    statistics.Add(temp);
                }
                return Ok(statistics);
            }
            catch {
                return BadRequest();
            }
        }
    }
}
