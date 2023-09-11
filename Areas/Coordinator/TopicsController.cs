using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;
using Microsoft.AspNetCore.Authorization;

namespace TCS2010PPTG4.Areas.Coordinator
{
    [Authorize(Roles = "Coordinator, Manager")]
    [Area("Coordinator")]
    public class TopicsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public TopicsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
            _context = context;
        }

        // GET: Coordinator/Topics
        public async Task<IActionResult> Index()
        {
            return View(await _context.Topic.ToListAsync());
        }

        // GET: Coordinator/Topics/Details/5
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

            return View(topic);
        }

        // GET: Coordinator/Topics/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Coordinator/Topics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Topic topic)
        {
            if (ModelState.IsValid)
            {
                if(topic.Deadline_1 <= topic.Deadline_2)
                {
                    topic.CreationDate = DateTime.Now;

                    _context.Add(topic);
                    await _context.SaveChangesAsync();

                    string webRootPath = _env.WebRootPath;
                    var folderName = topic.Id.ToString();

                    var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, folderName);

                    if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

                    return RedirectToAction(nameof(Index));
                }
                ViewData["Error"] = "Deadline 2 is not applicable.";
            }

            
            return View(topic);
        }

        // GET: Coordinator/Topics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topic.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            return View(topic);
        }

        // POST: Coordinator/Topics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Topic topic)
        {
            if (id != topic.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topic);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicExists(topic.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(topic);
        }

        // GET: Coordinator/Topics/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(topic);
        }

        // POST: Coordinator/Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topic.FindAsync(id);
            _context.Topic.Remove(topic);
            await _context.SaveChangesAsync();

            string webRootPath = _env.WebRootPath;
            var folderName = id.ToString();

            var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, folderName);

            if (Directory.Exists(path)) { Directory.Delete(path); }

            return RedirectToAction(nameof(Index));
        }

        private bool TopicExists(int id)
        {
            return _context.Topic.Any(e => e.Id == id);
        }
    }
}
