using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;
using Microsoft.AspNetCore.Authorization;

namespace TCS2010PPTG4.Areas.Student
{
    [Authorize(Roles = "Student")]
    [Area("Student")]
    public class TopicController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public TopicController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
            _context = context;
        }

        // GET: Topic
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // var user = await _context.Users.FindAsync(userId);
            // var departmentId = user.DepartmentId;

            //var currentDeptUserIds = await _context.Users.Where(u => u.DepartmentId == departmentId)
            //                                               .Select(u => u.Id)
            //                                                 .ToListAsync();
            var topicIds = await _context.Contribution.Where(c => c.ContributorId == userId)
                                                           .Select(c => c.TopicId)
                                                           .ToListAsync();



            var topics = await _context.Topic.Where(t => t.Deadline_2 >= DateTime.Now || topicIds.Contains(t.Id))
                                             .OrderByDescending(t => t.Deadline_2)
                                             .ToListAsync();

            return View(topics);
        }

        // GET: Topic/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topic
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentContribution = await _context.Contribution.Include(c => c.Files).FirstOrDefaultAsync(c => c.ContributorId == userId
                                                                                                       && c.TopicId == id);

            List<Comment> comments = null;
            if (currentContribution != null)
            {
                comments = await _context.Comment.Include(c => c.User)
                                                 .Where(c => c.ContributionId == currentContribution.Id)
                                                 .OrderBy(c => c.Date)
                                                 .ToListAsync();
            }

            ViewData["Comments"] = comments;
            ViewData["ContributorId"] = userId;
            ViewData["currentContribution"] = currentContribution;

            return View(topic);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Contribution contribution, IFormFile file)
        {
            var topic = await _context.Topic.FirstOrDefaultAsync(t => t.Id == contribution.TopicId);

            if (topic.Deadline_2 >= DateTime.Now)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ModelState.IsValid)
                {
                    var user = await _context.Users.FindAsync(userId);
                    var existContribution = await _context.Contribution.FirstOrDefaultAsync(c => c.ContributorId == userId && c.TopicId == contribution.TopicId);

                    if (existContribution == null)
                    {
                        contribution.ContributorId = userId;
                        contribution.Status = ContributionStatus.Pending;

                        _context.Add(contribution);
                        await _context.SaveChangesAsync();

                        existContribution = contribution;
                    }

                    else
                    {
                        existContribution.Status = ContributionStatus.Pending;

                        _context.Update(existContribution);
                        await _context.SaveChangesAsync();
                    }

                    if (file.Length > 0)
                    {
                        FileType? fileType;
                        string fileExtension = Path.GetExtension(file.FileName).ToLower();

                        switch (fileExtension)
                        {
                            case ".doc": case ".docx": fileType = FileType.Document; break;
                            case ".jpg": case ".png": fileType = FileType.Image; break;
                            default: fileType = null; break;
                        }

                        if (fileType != null)
                        {

                            //create folder
                            string webRootPath = _env.WebRootPath;
                            var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, existContribution.TopicId.ToString(), user.Number);
                            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                            // Upload file, create file
                            path = Path.Combine(path, String.Format("{0}.{1:yyyy-MM-dd.ss-mm-HH}{2}", user.Number, DateTime.Now, fileExtension));
                            using var stream = new FileStream(path, FileMode.Create);
                            file.CopyTo(stream);
                            var newFile = new SubmittedFile();
                            newFile.ContributionId = existContribution.Id;
                            newFile.URL = path;
                            newFile.Type = (FileType)fileType;
                            _context.Add(newFile);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            return RedirectToAction(nameof(Details), new { id = contribution.TopicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int topicId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contribution.FirstOrDefaultAsync(c => c.ContributorId == userId
                                                                                        && c.TopicId == topicId);

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

            return RedirectToAction(nameof(Details), new { id = topicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteComment(int commentId)
        {
            int topicId = 0;
            if (ModelState.IsValid)
            {
                var commented = await _context.Comment.FindAsync(commentId);
                var contribution = await _context.Contribution.FindAsync(commented.ContributionId);

                topicId = contribution.TopicId;
                _context.Remove(commented);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = topicId });
        }

        public async Task<ActionResult> DownloadFile(int fileId = -1)
        {
            var file = await _context.File.FindAsync(fileId);
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.URL);
            return File(fileBytes, MediaTypeNames.Application.Octet, Path.GetFileName(file.URL));
        }
    }
}
