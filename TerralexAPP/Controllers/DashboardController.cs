using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TerralexAPP.Data;
using TerralexAPP.Models;

namespace TerralexAPP.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var activeStatusIds = await _context.TransactionStatuses
                .Where(s => s.Name != "Closed" && !s.IsDeleted)
                .Select(s => s.TransactionStatusId)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalClients = await _context.Clients.CountAsync(c => !c.IsDeleted),
                TotalProperties = await _context.Properties.CountAsync(p => !p.IsDeleted),
                ActiveTransactions = await _context.Transactions.CountAsync(t => !t.IsDeleted && activeStatusIds.Contains(t.TransactionStatusId)),
                TotalRevenue = await _context.Payments.Where(p => !p.IsDeleted).SumAsync(p => p.TotalAmount),
                UpcomingAppointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Where(a => !a.IsDeleted && a.AppointmentDate >= DateTime.Today)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .ToListAsync(),
                RecentTransactions = await _context.Transactions
                    .Include(t => t.Client)
                    .Include(t => t.Property)
                    .Include(t => t.TransactionStatus)
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.StarDate)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        public IActionResult Clients()
        {
            ViewData["Title"] = "Clients";
            var clients = _context.Clients.Where(c => !c.IsDeleted).ToList();
            return View(clients);
        }

        public IActionResult CreateClient()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClient(Client client, IFormFile? IDImageFront, IFormFile? IDImageBack)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create uploads directory if it doesn't exist
                    string uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "clients");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    // Handle front ID image
                    if (IDImageFront != null && IDImageFront.Length > 0)
                    {
                        string frontFileName = $"{Guid.NewGuid()}_{IDImageFront.FileName}";
                        string frontFilePath = Path.Combine(uploadsPath, frontFileName);
                        using (var stream = new FileStream(frontFilePath, FileMode.Create))
                        {
                            await IDImageFront.CopyToAsync(stream);
                        }
                        client.IDImageFrontPath = $"/uploads/clients/{frontFileName}";
                    }

                    // Handle back ID image
                    if (IDImageBack != null && IDImageBack.Length > 0)
                    {
                        string backFileName = $"{Guid.NewGuid()}_{IDImageBack.FileName}";
                        string backFilePath = Path.Combine(uploadsPath, backFileName);
                        using (var stream = new FileStream(backFilePath, FileMode.Create))
                        {
                            await IDImageBack.CopyToAsync(stream);
                        }
                        client.IDImageBackPath = $"/uploads/clients/{backFileName}";
                    }

                    _context.Add(client);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Clients));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving client: {ex.Message}");
                }
            }
            return View(client);
        }

        public IActionResult ViewClient(int? id)
        {
            if (id == null)
                return NotFound();

            var client = _context.Clients.Find(id);
            if (client == null || client.IsDeleted)
                return NotFound();

            return View(client);
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult EditClient(int? id)
        {
            if (id == null)
                return NotFound();

            var client = _context.Clients.Find(id);
            if (client == null || client.IsDeleted)
                return NotFound();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> EditClient(int id, Client client, IFormFile? IDImageFront, IFormFile? IDImageBack)
        {
            if (id != client.ClientId)
                return NotFound();

            var existingClient = _context.Clients.Find(id);
            if (existingClient == null || existingClient.IsDeleted)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    string uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "clients");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    // Handle front ID image
                    if (IDImageFront != null && IDImageFront.Length > 0)
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(existingClient.IDImageFrontPath))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingClient.IDImageFrontPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }

                        string frontFileName = $"{Guid.NewGuid()}_{IDImageFront.FileName}";
                        string frontFilePath = Path.Combine(uploadsPath, frontFileName);
                        using (var stream = new FileStream(frontFilePath, FileMode.Create))
                        {
                            await IDImageFront.CopyToAsync(stream);
                        }
                        existingClient.IDImageFrontPath = $"/uploads/clients/{frontFileName}";
                    }

                    // Handle back ID image
                    if (IDImageBack != null && IDImageBack.Length > 0)
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(existingClient.IDImageBackPath))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingClient.IDImageBackPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }

                        string backFileName = $"{Guid.NewGuid()}_{IDImageBack.FileName}";
                        string backFilePath = Path.Combine(uploadsPath, backFileName);
                        using (var stream = new FileStream(backFilePath, FileMode.Create))
                        {
                            await IDImageBack.CopyToAsync(stream);
                        }
                        existingClient.IDImageBackPath = $"/uploads/clients/{backFileName}";
                    }

                    // Update other fields
                    existingClient.FirstName = client.FirstName;
                    existingClient.LastName = client.LastName;
                    existingClient.NationalId = client.NationalId;
                    existingClient.Mobile = client.Mobile;
                    existingClient.Email = client.Email;
                    existingClient.Address = client.Address;
                    existingClient.ClientType = client.ClientType;

                    _context.Update(existingClient);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Clients));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating client: {ex.Message}");
                }
            }
            return View(existingClient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = _context.Clients.Find(id);
            if (client == null)
                return NotFound();

            try
            {
                client.IsDeleted = true;
                _context.Update(client);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting client: {ex.Message}");
            }

            return RedirectToAction(nameof(Clients));
        }

        public IActionResult Properties()
        {
            ViewData["Title"] = "Properties";
            return View();
        }

        public IActionResult Documents()
        {
            ViewData["Title"] = "Documents";
            return View();
        }
    }
}
