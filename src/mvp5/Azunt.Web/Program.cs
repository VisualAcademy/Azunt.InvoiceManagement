using Azunt.Web.Settings;
using Azunt.Web.Billing.Data;
using Azunt.Web.Billing.Domain;
using Azunt.Web.Billing.Services;
using Azunt.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using Stripe;


var builder = WebApplication.CreateBuilder(args);



// -------------------------------------------------
// 1) Stripe 설정 바인딩 (Settings/StripeSettings.cs)
// -------------------------------------------------
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));




// QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// EF Core InMemory
builder.Services.AddDbContext<BillingDbContext>(opt => opt.UseInMemoryDatabase("BillingDemo"));

// ASP.NET Core Identity (In-Memory)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseInMemoryDatabase("AuthDemo"));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// DI registrations
builder.Services.AddSingleton<OutboxService>();
builder.Services.AddSingleton<InboxService>();

builder.Services.AddScoped<IInvoiceService, Azunt.Web.Billing.Services.InvoiceService>();
builder.Services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
builder.Services.AddScoped<IInvoicePdfService, QuestPdfInvoiceService>();
builder.Services.AddSingleton<OutboxService>();
builder.Services.AddScoped<IFileStorage>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var webRoot = env.WebRootPath;
    if (string.IsNullOrEmpty(webRoot))
    {
        webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRoot);
    }
    var root = Path.Combine(webRoot, "invoicefiles");
    Directory.CreateDirectory(root);
    return new FileSystemStorage(root, "/invoicefiles");
});
builder.Services.AddScoped<IEmailSender, FakeEmailSender>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o => o.DetailedErrors = true);

var app = builder.Build();





// -------------------------------------------------
// 2) Stripe SecretKey 전역 설정 (StripeConfiguration.ApiKey)
// -------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var stripeOptions = scope.ServiceProvider
        .GetRequiredService<IOptions<StripeSettings>>().Value;

    StripeConfiguration.ApiKey = stripeOptions.SecretKey;
}





// Seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

    var tenants = new[] { "Hawaso", "VisualAcademy" };

    long clientId = 1;
    long invoiceId = 1001;
    long itemId = 1;

    foreach (var tenant in tenants)
    {
        // 3 clients per tenant with different types
        var vendor = new Azunt.Web.Billing.Domain.Client
        {
            Id = clientId++,
            TenantId = tenant,
            OrganizationName = $"{tenant} Vendor",
            BillingEmail = $"billing@{tenant.ToLowerInvariant()}.vendor.test",
            Domain = $"{tenant.ToLowerInvariant()}.vendor.test",
            Type = ClientType.Vendor
        };

        var vendorEmployee = new Azunt.Web.Billing.Domain.Client
        {
            Id = clientId++,
            TenantId = tenant,
            OrganizationName = $"{tenant} Vendor Employee",
            BillingEmail = $"employee@{tenant.ToLowerInvariant()}.vendor.test",
            Domain = $"{tenant.ToLowerInvariant()}.vendor.test",
            Type = ClientType.VendorEmployee
        };

        var internalEmployee = new Azunt.Web.Billing.Domain.Client
        {
            Id = clientId++,
            TenantId = tenant,
            OrganizationName = $"{tenant} Internal",
            BillingEmail = $"internal@{tenant.ToLowerInvariant()}.corp.test",
            Domain = $"{tenant.ToLowerInvariant()}.corp.test",
            Type = ClientType.Employee
        };

        db.Clients.AddRange(vendor, vendorEmployee, internalEmployee);

        // One sequence per tenant starting at 1
        db.InvoiceNumberSequences.Add(new InvoiceNumberSequence
        {
            TenantId = tenant,
            NextValue = 1
        });

        // One draft invoice per client
        foreach (var client in new[] { vendor, vendorEmployee, internalEmployee })
        {
            var inv = new Azunt.Web.Billing.Domain.Invoice
            {
                Id = invoiceId++,
                TenantId = tenant,
                ClientId = client.Id,
                TenantName = tenant,
                TenantKey = tenant,
                Email = client.BillingEmail ?? string.Empty,
                EmailNormalized = (client.BillingEmail ?? string.Empty).Trim().ToLowerInvariant(),
                ClientName = client.OrganizationName,
                ClientType = client.Type.ToString(),
                FirstName = client.OrganizationName,
                MiddleName = null,
                LastName = null,
                Currency = "USD"
            };

            inv.Items.Add(new Azunt.Web.Billing.Domain.InvoiceItem
            {
                Id = itemId++,
                InvoiceId = inv.Id,
                Description = "Azure compute hours",
                Quantity = 10,
                UnitPrice = 12
            });

            inv.Items.Add(new Azunt.Web.Billing.Domain.InvoiceItem
            {
                Id = itemId++,
                InvoiceId = inv.Id,
                Description = "Support plan",
                Quantity = 1,
                UnitPrice = 33
            });

            inv.RecalculateTotals();
            db.Invoices.Add(inv);
        }
    }

    await db.SaveChangesAsync();
}

// Seed demo users for Identity
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var users = new[]
    {
        new ApplicationUser { UserName = "hawaso@test.local", Email = "hawaso@test.local", TenantName = "Hawaso" },
        new ApplicationUser { UserName = "visualacademy@test.local", Email = "visualacademy@test.local", TenantName = "VisualAcademy" }
    };

    foreach (var u in users)
    {
        if (await userManager.FindByNameAsync(u.UserName!) is null)
        {
            // simple demo password
            await userManager.CreateAsync(u, "Pass123$");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");
app.Run();
