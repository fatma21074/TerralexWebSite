using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class DocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Documents
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Document Center";

            var transactionDocs = await _context.TransactionDocuments
                .Include(d => d.Transaction).ThenInclude(t => t.Client)
                .Include(d => d.Transaction).ThenInclude(t => t.Property)
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.TransactionDocument_Id)
                .ToListAsync();

            var clientDocs = await _context.ClientDocuments
                .Include(d => d.Client)
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.ClientDocumentId)
                .ToListAsync();

            var propertyDocs = await _context.PropertyDocumnets
                .Include(d => d.Property)
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.PropertyDocumentId)
                .ToListAsync();

            ViewBag.TransactionDocs = transactionDocs;
            ViewBag.ClientDocs = clientDocs;
            ViewBag.PropertyDocs = propertyDocs;

            // Populate upload dropdowns
            ViewBag.Clients = new SelectList(await _context.Clients.Where(c => !c.IsDeleted).ToListAsync(), "ClientId", "FirstName");
            ViewBag.Properties = new SelectList(await _context.Properties.Where(p => !p.IsDeleted).ToListAsync(), "PropertyId", "District");
            ViewBag.Transactions = new SelectList(await _context.Transactions.Where(t => !t.IsDeleted).Select(t => new { t.TransactionId, Text = $"Transaction #{t.TransactionId} - Client: {t.Client.FirstName}" }).ToListAsync(), "TransactionId", "Text");

            return View();
        }

        // POST: Documents/UploadClientDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadClientDocument(int clientId, string documentName, IFormFile file, DateTime? renewalDate)
        {
            if (file != null && file.Length > 0 && !string.IsNullOrEmpty(documentName))
            {
                try
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "clients");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    string filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var clientDoc = new ClientDocument
                    {
                        ClientId = clientId,
                        DocumentName = documentName,
                        CreateDate = DateTime.Now,
                        RenewalDate = renewalDate,
                        IsDeleted = false
                    };

                    // We can also save the file path to client model if needed, but saving in ClientDocument table is sufficient.
                    _context.ClientDocuments.Add(clientDoc);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Failed to upload: {ex.Message}";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Documents/UploadPropertyDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPropertyDocument(int propertyId, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "properties");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    string filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var propDoc = new PropertyDocumnet
                    {
                        PropertyId = propertyId,
                        DoucmentPath = $"/uploads/properties/{fileName}",
                        IsDeleted = false
                    };

                    _context.PropertyDocumnets.Add(propDoc);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Failed to upload: {ex.Message}";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
