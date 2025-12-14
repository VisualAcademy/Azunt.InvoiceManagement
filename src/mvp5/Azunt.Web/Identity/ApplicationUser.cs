using Microsoft.AspNetCore.Identity;

namespace Azunt.Web.Identity;

public class ApplicationUser : IdentityUser
{
    public string TenantName { get; set; } = string.Empty;
}
