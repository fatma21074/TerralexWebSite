using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TerralexAPP.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can Access and manage roles 
    public class RolesController : Controller
    {
        // Injecting RoleManager to manage roles
        private readonly RoleManager<ApplicationRole> _roleManager;
        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }
        //Display list of all roles
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }
        //Create a new role
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Add a new role with details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationRole role)
        {
            if (ModelState.IsValid)
            {
                role.CreatedAt = DateTime.UtcNow;
                role.CreatedBy = User.Identity?.Name ?? "System";
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Added Role Successfully...";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(role);
        }

        // Edit existing role details   
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return NotFound();
            return View(role);
        }
        // Update role details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApplicationRole role)
        {
            if (id != role.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingRole = await _roleManager.FindByIdAsync(id.ToString());
                if (existingRole == null) return NotFound();

                existingRole.Name = role.Name;
                existingRole.Description = role.Description;

                var result = await _roleManager.UpdateAsync(existingRole);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Updated Role Successfully...";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(role);
        }

        // Delete a role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
                TempData["Success"] = "Deleted Role Successfully...";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

