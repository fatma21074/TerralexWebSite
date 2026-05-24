using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TerralexAPP.Data;
using TerralexAPP.ViewModels;

namespace TerralexAPP.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {model.Email} logged in successfully.");
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    _logger.LogWarning($"Invalid login attempt for user {model.Email}.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true // Auto-confirm for development
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "Client");
                    
                    _logger.LogInformation($"User {model.Email} registered successfully.");
                    TempData["RegisterSuccess"] = "Registration successful! Please login with your credentials.";
                    return RedirectToAction(nameof(Login));
                }

                var errors = string.Join("<br/>", result.Errors.Select(e => e.Description));
                TempData["RegisterError"] = errors;
                _logger.LogWarning($"Registration failed for {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            else
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["RegisterError"] = errors;
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"User {user} logged out.");
            return RedirectToAction("Index", "Website");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
