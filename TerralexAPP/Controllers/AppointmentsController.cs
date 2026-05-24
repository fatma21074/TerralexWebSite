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
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        public async Task<IActionResult> Index(int? clientId, string? type)
        {
            ViewData["Title"] = "Appointments Calendar";
            
            var query = _context.Appointments
                .Include(a => a.Client)
                .Where(a => !a.IsDeleted);

            if (clientId.HasValue)
            {
                query = query.Where(a => a.ClientId == clientId.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(a => a.AppointmentType == type);
            }

            var appointments = await query.OrderBy(a => a.AppointmentDate).ToListAsync();
            
            // Fetch staff details to map to the view
            var staffList = await _context.OfficeStuffs.Where(s => !s.IsDeleted).ToListAsync();
            ViewBag.Staffs = staffList.ToDictionary(s => s.StuffId, s => $"{s.Name} ({s.JobTitle})");

            var clients = await _context.Clients.Where(c => !c.IsDeleted)
                .Select(c => new { ClientId = c.ClientId, Name = $"{c.FirstName} {c.LastName}" })
                .ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name");

            return View(appointments);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(m => m.AppointmentId == id && !m.IsDeleted);

            if (appointment == null) return NotFound();

            var staff = await _context.OfficeStuffs.FindAsync(appointment.StuffId);
            ViewBag.StaffName = staff != null ? $"{staff.Name} ({staff.JobTitle})" : "Unknown";

            return View(appointment);
        }

        // GET: Appointments/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Schedule Appointment";
            await PopulateDropdowns();
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.IsDeleted = false;
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdowns(appointment);
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null || appointment.IsDeleted) return NotFound();

            ViewData["Title"] = "Edit Appointment";
            await PopulateDropdowns(appointment);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId))
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
            await PopulateDropdowns(appointment);
            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.IsDeleted = true;
                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(Appointment? appointment = null)
        {
            var clients = await _context.Clients.Where(c => !c.IsDeleted)
                .Select(c => new { ClientId = c.ClientId, Name = $"{c.FirstName} {c.LastName}" })
                .ToListAsync();

            var staff = await _context.OfficeStuffs.Where(s => !s.IsDeleted)
                .Select(s => new { StuffId = s.StuffId, Name = $"{s.Name} ({s.JobTitle})" })
                .ToListAsync();

            ViewBag.ClientId = new SelectList(clients, "ClientId", "Name", appointment?.ClientId);
            ViewBag.StuffId = new SelectList(staff, "StuffId", "Name", appointment?.StuffId);
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id && !e.IsDeleted);
        }
    }
}
