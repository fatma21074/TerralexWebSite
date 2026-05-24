using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TerralexAPP.Data;
using TerralexApp.Models;

namespace TerralexAPP.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Financial Transactions";

            var payments = await _context.Payments
                .Include(p => p.Transaction).ThenInclude(t => t.Client)
                .Include(p => p.Transaction).ThenInclude(t => t.Property)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Calculate total financial metrics for summary cards
            ViewBag.TotalRevenue = payments.Sum(p => p.TotalAmount);
            ViewBag.TotalTax = payments.Sum(p => p.TaxValue ?? 0);
            ViewBag.ServiceRevenue = payments.Sum(p => p.AmountService);

            return View(payments);
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Transaction).ThenInclude(t => t.Client)
                .Include(p => p.Transaction).ThenInclude(t => t.Property)
                .FirstOrDefaultAsync(m => m.PaymentId == id && !m.IsDeleted);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // GET: Payments/Create
        public async Task<IActionResult> Create(int? transactionId)
        {
            ViewData["Title"] = "Record Payment Receipt";
            await PopulateTransactionsDropdown(transactionId);
            return View();
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (ModelState.IsValid)
            {
                // Auto-calculate TotalAmount based on Service amount and Tax (default to 14% if null)
                if (payment.TaxValue == null || payment.TaxValue == 0)
                {
                    payment.TaxValue = Math.Round(payment.AmountService * 0.14m, 2);
                }
                payment.TotalAmount = payment.AmountService + (payment.TaxValue ?? 0);
                payment.IsDeleted = false;
                payment.PaymentDate = DateTime.Now;

                _context.Add(payment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await PopulateTransactionsDropdown(payment.TransactionId);
            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                payment.IsDeleted = true;
                _context.Update(payment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateTransactionsDropdown(int? selectedTransactionId = null)
        {
            // Only list active transactions that DO NOT already have a payment associated with them
            var paidTransactionIds = await _context.Payments
                .Where(p => !p.IsDeleted)
                .Select(p => p.TransactionId)
                .ToListAsync();

            var query = _context.Transactions
                .Include(t => t.Client)
                .Where(t => !t.IsDeleted);

            // If we are editing or have a pre-selected transaction, allow it in the list
            if (selectedTransactionId.HasValue)
            {
                query = query.Where(t => !paidTransactionIds.Contains(t.TransactionId) || t.TransactionId == selectedTransactionId.Value);
            }
            else
            {
                query = query.Where(t => !paidTransactionIds.Contains(t.TransactionId));
            }

            var unpaidTransactions = await query
                .Select(t => new {
                    TransactionId = t.TransactionId,
                    Description = $"Transaction #{t.TransactionId} - Client: {t.Client.FirstName} {t.Client.LastName} (Total Fees: {t.TotalFees})"
                })
                .ToListAsync();

            ViewBag.TransactionId = new SelectList(unpaidTransactions, "TransactionId", "Description", selectedTransactionId);
        }
    }
}
