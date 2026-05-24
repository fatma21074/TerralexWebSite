using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using TerralexAPP.Data;
using TerralexApp.Models;

namespace TerralexAPP.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropertiesController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Properties
        public async Task<IActionResult> Index(string searchString, int? cityId, int? propertyTypeId)
        {
            var query = _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Address.Contains(searchString) || p.District.Contains(searchString));
            }

            if (cityId.HasValue)
            {
                query = query.Where(p => p.CityId == cityId.Value);
            }

            if (propertyTypeId.HasValue)
            {
                query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => !c.IsDeleted).ToListAsync(), "CityId", "Name");
            ViewBag.PropertyTypes = new SelectList(await _context.PropertyTypes.Where(t => !t.IsDeleted).ToListAsync(), "PropertyTypeId", "Name");

            var properties = await query.ToListAsync();
            return View(properties);
        }

        // GET: Properties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyDocumnets)
                .Include(p => p.Transactions).ThenInclude(t => t.Client)
                .Include(p => p.Transactions).ThenInclude(t => t.TransactionStatus)
                .FirstOrDefaultAsync(m => m.PropertyId == id && !m.IsDeleted);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // GET: Properties/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => !c.IsDeleted).ToListAsync(), "CityId", "Name");
            ViewBag.PropertyTypes = new SelectList(await _context.PropertyTypes.Where(t => !t.IsDeleted).ToListAsync(), "PropertyTypeId", "Name");
            return View();
        }

        // POST: Properties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property, List<IFormFile> imageFiles, List<IFormFile> documentFiles)
        {
            // Default "Documents" if empty because it's required (max length 10 in DB)
            if (string.IsNullOrEmpty(property.Documents))
            {
                property.Documents = "Active";
            }

            if (ModelState.IsValid)
            {
                _context.Add(property);
                await _context.SaveChangesAsync();

                // Save Images
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    string imagesDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties", "images");
                    if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                    foreach (var file in imageFiles)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            string filePath = Path.Combine(imagesDir, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var propImage = new PropertyImage
                            {
                                PropertyId = property.PropertyId,
                                ImagePath = $"/uploads/properties/images/{fileName}",
                                IsDeleted = false
                            };
                            _context.PropertyImages.Add(propImage);
                        }
                    }
                }

                // Save Documents
                if (documentFiles != null && documentFiles.Count > 0)
                {
                    string docsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties", "documents");
                    if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);

                    foreach (var file in documentFiles)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            string filePath = Path.Combine(docsDir, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var propDoc = new PropertyDocumnet
                            {
                                PropertyId = property.PropertyId,
                                DoucmentPath = $"/uploads/properties/documents/{fileName}",
                                IsDeleted = false
                            };
                            _context.PropertyDocumnets.Add(propDoc);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => !c.IsDeleted).ToListAsync(), "CityId", "Name", property.CityId);
            ViewBag.PropertyTypes = new SelectList(await _context.PropertyTypes.Where(t => !t.IsDeleted).ToListAsync(), "PropertyTypeId", "Name", property.PropertyTypeId);
            return View(property);
        }

        // GET: Properties/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyDocumnets)
                .FirstOrDefaultAsync(p => p.PropertyId == id && !p.IsDeleted);

            if (property == null)
            {
                return NotFound();
            }

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => !c.IsDeleted).ToListAsync(), "CityId", "Name", property.CityId);
            ViewBag.PropertyTypes = new SelectList(await _context.PropertyTypes.Where(t => !t.IsDeleted).ToListAsync(), "PropertyTypeId", "Name", property.PropertyTypeId);
            return View(property);
        }

        // POST: Properties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, Property property, List<IFormFile> newImageFiles, List<IFormFile> newDocumentFiles)
        {
            if (id != property.PropertyId)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(property.Documents))
            {
                property.Documents = "Active";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(property);
                    await _context.SaveChangesAsync();

                    // Upload new images
                    if (newImageFiles != null && newImageFiles.Count > 0)
                    {
                        string imagesDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties", "images");
                        if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                        foreach (var file in newImageFiles)
                        {
                            if (file.Length > 0)
                            {
                                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                                string filePath = Path.Combine(imagesDir, fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var propImage = new PropertyImage
                                {
                                    PropertyId = property.PropertyId,
                                    ImagePath = $"/uploads/properties/images/{fileName}",
                                    IsDeleted = false
                                };
                                _context.PropertyImages.Add(propImage);
                            }
                        }
                    }

                    // Upload new documents
                    if (newDocumentFiles != null && newDocumentFiles.Count > 0)
                    {
                        string docsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties", "documents");
                        if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);

                        foreach (var file in newDocumentFiles)
                        {
                            if (file.Length > 0)
                            {
                                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                                string filePath = Path.Combine(docsDir, fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var propDoc = new PropertyDocumnet
                                {
                                    PropertyId = property.PropertyId,
                                    DoucmentPath = $"/uploads/properties/documents/{fileName}",
                                    IsDeleted = false
                                };
                                _context.PropertyDocumnets.Add(propDoc);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyId))
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

            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => !c.IsDeleted).ToListAsync(), "CityId", "Name", property.CityId);
            ViewBag.PropertyTypes = new SelectList(await _context.PropertyTypes.Where(t => !t.IsDeleted).ToListAsync(), "PropertyTypeId", "Name", property.PropertyTypeId);
            return View(property);
        }

        // POST: Properties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                property.IsDeleted = true;
                _context.Properties.Update(property);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteImage(int imageId, int propertyId)
        {
            var image = await _context.PropertyImages.FindAsync(imageId);
            if (image != null)
            {
                image.IsDeleted = true;
                _context.PropertyImages.Update(image);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = propertyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteDocument(int documentId, int propertyId)
        {
            var doc = await _context.PropertyDocumnets.FindAsync(documentId);
            if (doc != null)
            {
                doc.IsDeleted = true;
                _context.PropertyDocumnets.Update(doc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = propertyId });
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id && !e.IsDeleted);
        }
    }
}
