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
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TransactionsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Transactions
        public async Task<IActionResult> Index(int? clientId, int? statusId)
        {
            var query = _context.Transactions
                .Include(t => t.Client)
                .Include(t => t.Property)
                .Include(t => t.ServiceType)
                .Include(t => t.Stuff)
                .Include(t => t.TransactionStatus)
                .Where(t => !t.IsDeleted);

            if (clientId.HasValue)
            {
                query = query.Where(t => t.ClientId == clientId.Value);
            }

            if (statusId.HasValue)
            {
                query = query.Where(t => t.TransactionStatusId == statusId.Value);
            }

            var clients = await _context.Clients.Where(c => !c.IsDeleted)
                .Select(c => new { ClientId = c.ClientId, Name = c.FirstName + " " + c.LastName })
                .ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name");
            ViewBag.Statuses = new SelectList(await _context.TransactionStatuses.Where(s => !s.IsDeleted).ToListAsync(), "TransactionStatusId", "Name");

            var transactions = await query.ToListAsync();
            return View(transactions);
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Client)
                .Include(t => t.Property)
                .Include(t => t.ServiceType)
                .Include(t => t.Stuff)
                .Include(t => t.TransactionStatus)
                .Include(t => t.TransactionsStages)
                .Include(t => t.TransactionFees)
                .Include(t => t.TransactionDocuments).ThenInclude(td => td.Template)
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(m => m.TransactionId == id && !m.IsDeleted);

            if (transaction == null)
            {
                return NotFound();
            }

            // Populate Templates dropdown for attaching new documents
            ViewBag.Templates = new SelectList(await _context.Templates.Where(t => !t.IsDeleted).ToListAsync(), "TemplateId", "TemplateName");

            return View(transaction);
        }

        // GET: Transactions/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transaction);
                await _context.SaveChangesAsync();

                // Automatically seed transaction workflow stages
                var today = DateTime.Now;
                var stages = new List<TransactionsStage>
                {
                    new TransactionsStage
                    {
                        TransactionId = transaction.TransactionId,
                        StageName = "Contract Preparation",
                        StageStatus = false,
                        StartDate = today,
                        ExpectedEndDate = today.AddDays(5),
                        CompletedDate = today,
                        Notes = "Drafting initial real estate contract.",
                        IsDeleted = false
                    },
                    new TransactionsStage
                    {
                        TransactionId = transaction.TransactionId,
                        StageName = "Legal Review",
                        StageStatus = false,
                        StartDate = today.AddDays(5),
                        ExpectedEndDate = today.AddDays(10),
                        CompletedDate = today,
                        Notes = "Conducting legal review of property deeds.",
                        IsDeleted = false
                    },
                    new TransactionsStage
                    {
                        TransactionId = transaction.TransactionId,
                        StageName = "Signing",
                        StageStatus = false,
                        StartDate = today.AddDays(10),
                        ExpectedEndDate = today.AddDays(12),
                        CompletedDate = today,
                        Notes = "Client signature scheduling.",
                        IsDeleted = false
                    },
                    new TransactionsStage
                    {
                        TransactionId = transaction.TransactionId,
                        StageName = "Documentation",
                        StageStatus = false,
                        StartDate = today.AddDays(12),
                        ExpectedEndDate = today.AddDays(15),
                        CompletedDate = today,
                        Notes = "Filing legal transaction affidavits.",
                        IsDeleted = false
                    },
                    new TransactionsStage
                    {
                        TransactionId = transaction.TransactionId,
                        StageName = "Registration",
                        StageStatus = false,
                        StartDate = today.AddDays(15),
                        ExpectedEndDate = today.AddDays(30),
                        CompletedDate = today,
                        Notes = "Final registration at city registry.",
                        IsDeleted = false
                    }
                };

                _context.TransactionsStages.AddRange(stages);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(transaction);
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null || transaction.IsDeleted)
            {
                return NotFound();
            }

            await PopulateDropdowns(transaction);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            if (id != transaction.TransactionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.TransactionId))
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

            await PopulateDropdowns(transaction);
            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                transaction.IsDeleted = true;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Transactions/UpdateStageStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStageStatus(int stageId, int transactionId, bool isCompleted, string notes)
        {
            var stage = await _context.TransactionsStages.FindAsync(stageId);
            if (stage != null)
            {
                stage.StageStatus = isCompleted;
                stage.CompletedDate = DateTime.Now;
                if (!string.IsNullOrEmpty(notes))
                {
                    stage.Notes = notes;
                }
                _context.Update(stage);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = transactionId });
        }

        // POST: Transactions/AddFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFee(int transactionId, string itemName, decimal fees)
        {
            if (!string.IsNullOrEmpty(itemName) && fees >= 0)
            {
                var fee = new TransactionFee
                {
                    TransactionId = transactionId,
                    ItemName = itemName,
                    Fees = fees,
                    IsDeleted = false
                };
                _context.TransactionFees.Add(fee);

                // Update Transaction's TotalFees
                var trans = await _context.Transactions.FindAsync(transactionId);
                if (trans != null)
                {
                    trans.TotalFees = (trans.TotalFees ?? 0) + fees;
                    _context.Update(trans);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = transactionId });
        }

        // POST: Transactions/DeleteFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFee(int feeId, int transactionId)
        {
            var fee = await _context.TransactionFees.FindAsync(feeId);
            if (fee != null)
            {
                fee.IsDeleted = true;
                _context.TransactionFees.Update(fee);

                // Deduct from Transaction's TotalFees
                var trans = await _context.Transactions.FindAsync(transactionId);
                if (trans != null)
                {
                    trans.TotalFees = Math.Max(0, (trans.TotalFees ?? 0) - fee.Fees);
                    _context.Update(trans);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = transactionId });
        }

        // POST: Transactions/AddDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocument(int transactionId, string documentName, string documentType, int templateId, IFormFile file, string description, string notes)
        {
            if (file != null && file.Length > 0 && !string.IsNullOrEmpty(documentName))
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "transactions");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var doc = new TransactionDocument
                {
                    TransactionId = transactionId,
                    DocumentName = documentName,
                    DocumentType = documentType ?? "Contract File",
                    TemplateId = templateId,
                    FilePath = $"/uploads/transactions/{fileName}",
                    Description = description ?? "Transaction document",
                    Notes = notes ?? "No additional notes",
                    IsDeleted = false
                };

                _context.TransactionDocuments.Add(doc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = transactionId });
        }

        // POST: Transactions/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteDocument(int documentId, int transactionId)
        {
            var doc = await _context.TransactionDocuments.FindAsync(documentId);
            if (doc != null)
            {
                doc.IsDeleted = true;
                _context.TransactionDocuments.Update(doc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = transactionId });
        }

        private async Task PopulateDropdowns(Transaction? selectedTransaction = null)
        {
            var clients = await _context.Clients.Where(c => !c.IsDeleted)
                .Select(c => new { ClientId = c.ClientId, Name = c.FirstName + " " + c.LastName })
                .ToListAsync();

            var properties = await _context.Properties.Where(p => !p.IsDeleted)
                .Select(p => new { PropertyId = p.PropertyId, Address = p.District + " - " + p.Address + " (" + p.Area + " Sqm)" })
                .ToListAsync();

            var lawyers = await _context.OfficeStuffs.Where(s => !s.IsDeleted)
                .Select(s => new { StuffId = s.StuffId, Name = s.Name + " (" + s.JobTitle + ")" })
                .ToListAsync();

            var serviceTypes = await _context.ServerTypes.Where(s => !s.IsDeleted).ToListAsync();
            var statuses = await _context.TransactionStatuses.Where(s => !s.IsDeleted).ToListAsync();

            ViewBag.ClientId = new SelectList(clients, "ClientId", "Name", selectedTransaction?.ClientId);
            ViewBag.PropertyId = new SelectList(properties, "PropertyId", "Address", selectedTransaction?.PropertyId);
            ViewBag.StuffId = new SelectList(lawyers, "StuffId", "Name", selectedTransaction?.StuffId);
            ViewBag.ServiceTypeId = new SelectList(serviceTypes, "ServiceTypeId", "Name", selectedTransaction?.ServiceTypeId);
            ViewBag.TransactionStatusId = new SelectList(statuses, "TransactionStatusId", "Name", selectedTransaction?.TransactionStatusId);
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.TransactionId == id && !e.IsDeleted);
        }
    }
}
