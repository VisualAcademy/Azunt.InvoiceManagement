using Azunt.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Controllers;

[Route("Auth")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl = "/")
    {
        var result = await _signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // login failed
        return LocalRedirect("/login?error=1");
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout(string? returnUrl = "/")
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect(returnUrl ?? "/");
    }
}
