using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;
using Microsoft.AspNetCore.Authorization;

namespace TCS2010PPTG4.Areas.Coordinator
{
    [Authorize(Roles = "Coordinator, Manager")]
    [Area("Coordinator")]
    public class ContributionsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        public ContributionsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
            _context = context;
        }

        // GET: Coordinator/Contributions
        public async Task<IActionResult> Index(int topicId)
        {
            var contributions = await _context.Contribution.Include(c => c.Topic)
                                                           .Include(c => c.Contributor)
                                                           .Where(c => c.TopicId == topicId)
                                                           .ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleId = await _context.UserRoles.Where(u => u.UserId == userId)
                                           .Select(u => u.RoleId).FirstOrDefaultAsync();
            ViewData["TopicId"] = topicId;
            if (contributions != null)
            {
                if (roleId == "Manager")
                {
                    contributions = contributions.Where(c => c.Status == ContributionStatus.Approved).ToList();
                }
                else if (roleId == "Coordinator")
                {
                    var user = await _context.Users.FindAsync(userId);
                    contributions = contributions.Where(c => c.Contributor.DepartmentId == user.DepartmentId).ToList();
                }
            }

            return View(contributions);
        }

        // GET: Coordinator/Contributions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contribution
                .Include(c => c.Contributor)
                .Include(c => c.Topic)
                .Include(c => c.Files)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contribution == null)
            {
                return NotFound();
            }

            var comments = await _context.Comment.Include(c => c.User)
                                                .Where(c => c.ContributionId == id)
                                                .OrderBy(c => c.Date)
                                                .ToListAsync();



            ViewData["Comments"] = comments;

            return View(contribution);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int contributionId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contribution.FindAsync(contributionId);

                if (existContribution != null && !String.IsNullOrEmpty(commentContent))
                {
                    var comment = new Comment();

                    comment.Content = commentContent;
                    comment.Date = DateTime.Now;
                    comment.ContributionId = existContribution.Id;
                    comment.UserId = userId;

                    _context.Add(comment);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = contributionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mark(int contributionId, ContributionStatus contributionStatus)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var contribution = await _context.Contribution.Include(c => c.Contributor).Include(c => c.Topic)
                        .FirstOrDefaultAsync(c => c.Id == contributionId);
                    contribution.Status = contributionStatus;

                    _context.Update(contribution);
                    await _context.SaveChangesAsync();

                    var contributorFullname = $"{contribution.Contributor.FirstName} {contribution.Contributor.LastName}";

                    MailboxAddress from = new MailboxAddress("GreenTea School", "notrealreki@gmail.com");
                    MailboxAddress to = new MailboxAddress(contributorFullname, contribution.Contributor.Email);

                    BodyBuilder bodyBuilder = new BodyBuilder();
                    bodyBuilder.TextBody = $"Hello {contributorFullname}, \n\n" +
                                           $"Your contribution for {contribution.Topic.Title} is {contributionStatus}.\n\n" +
                                           $"";
                    MimeMessage message = new MimeMessage();
                    message.From.Add(from);
                    message.To.Add(to);
                    message.Subject = $"Contribution for {contribution.Topic.Title} Status";
                    message.Body = bodyBuilder.ToMessageBody();

                    SmtpClient client = new SmtpClient();
                    client.Connect("smtp.gmail.com", 465, true);
                    client.Authenticate("notrealreki", "msfpkzgomcowvbrp");

                    client.Send(message);
                    client.Disconnect(true);
                    client.Dispose();
                }
                catch (DbUpdateConcurrencyException)
                {
                    //if {!ContributionExists(contributionId)} { return NotFound(); }
                    //else { throw; }
                }
            }

            return RedirectToAction(nameof(Details), new { id = contributionId });
        }
        public async Task<ActionResult> DownloadApprovedFile(int topicId = -1)
        {
            var approvedContributions = await _context.Contribution.Include(c => c.Contributor)
                                                                   .Include(c => c.Files)
                                                                   .Where(c => c.TopicId == topicId
                                                                            && c.Status == ContributionStatus.Approved)
                                                                   .ToListAsync();
            if (approvedContributions.Count() > 0)
            {
                var topic = await _context.Topic.FindAsync(topicId);
                var zipPath = Path.Combine(_env.WebRootPath, _Global.PATH_TOPIC, topicId.ToString(), topic.Title + ".zip");

                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive achive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        foreach (var contribution in approvedContributions)
                        {
                            foreach (var file in contribution.Files)
                            {
                                achive.CreateEntryFromFile(file.URL, Path.Combine(contribution.Contributor.Number
                                                                                    , file.URL.Split("\\").Last()));
                            }
                        }
                    }
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(zipPath);

                System.IO.File.Delete(zipPath);


                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Zip, zipPath.Split("\\").Last());
            }

            return NoContent();
        }
    }
}

