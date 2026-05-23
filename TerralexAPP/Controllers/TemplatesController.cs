using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TerralexAPP.Data;
using TerralexApp.Models;

namespace TerralexAPP.Controllers
{
    [Authorize]
    public class TemplatesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TemplatesController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Templates
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Contract Templates";
            var templates = await _context.Templates
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.TemplateName)
                .ToListAsync();
            return View(templates);
        }

        // GET: Templates/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Upload Contract Template";
            return View();
        }

        // POST: Templates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Template template, IFormFile templateFile)
        {
            if (ModelState.IsValid || (template.TemplateName != null && templateFile != null))
            {
                if (templateFile != null && templateFile.Length > 0)
                {
                    try
                    {
                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "templates");
                        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(templateFile.FileName)}";
                        string filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await templateFile.CopyToAsync(stream);
                        }

                        template.TemplateContentPath = $"/uploads/templates/{fileName}";
                        template.IsDeleted = false;

                        _context.Add(template);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Failed to save file: {ex.Message}");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Please upload a valid template file.");
                }
            }
            return View(template);
        }

        // GET: Templates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var template = await _context.Templates.FindAsync(id);
            if (template == null || template.IsDeleted) return NotFound();

            ViewData["Title"] = "Edit Template";
            return View(template);
        }

        // POST: Templates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Template template, IFormFile? templateFile)
        {
            if (id != template.TemplateId) return NotFound();

            var existingTemplate = await _context.Templates.FindAsync(id);
            if (existingTemplate == null || existingTemplate.IsDeleted) return NotFound();

            if (ModelState.IsValid || template.TemplateName != null)
            {
                try
                {
                    existingTemplate.TemplateName = template.TemplateName;

                    if (templateFile != null && templateFile.Length > 0)
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(existingTemplate.TemplateContentPath))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingTemplate.TemplateContentPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "templates");
                        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(templateFile.FileName)}";
                        string filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await templateFile.CopyToAsync(stream);
                        }

                        existingTemplate.TemplateContentPath = $"/uploads/templates/{fileName}";
                    }

                    _context.Update(existingTemplate);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving updates: {ex.Message}");
                }
            }
            return View(template);
        }

        // POST: Templates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var template = await _context.Templates.FindAsync(id);
            if (template != null)
            {
                template.IsDeleted = true;
                _context.Update(template);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
