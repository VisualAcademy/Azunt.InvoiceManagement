using Azunt.Web.Billing.Data;
using Azunt.Web.Billing.Domain;
using Azunt.Web.Billing.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// EF Core InMemory
builder.Services.AddDbContext<BillingDbContext>(opt => opt.UseInMemoryDatabase("BillingDemo"));

// DI registrations
builder.Services.AddSingleton<OutboxService>();
builder.Services.AddSingleton<InboxService>();

builder.Services.AddScoped<IInvoiceService, InvoiceService>();
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
    var root = Path.Combine(webRoot, "invoices");
    Directory.CreateDirectory(root);
    return new FileSystemStorage(root, "/invoices");
});
builder.Services.AddScoped<IEmailSender, FakeEmailSender>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o => o.DetailedErrors = true);

var app = builder.Build();

// Seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    var tenant = "TENANT-DEMO";

    db.Customers.AddRange(
        new Customer { Id = 1, TenantId = tenant, OrganizationName = "Hawaso Vendor Co.", BillingEmail = "ap@vendor.test", Domain = "vendor.test", Type = CustomerType.Vendor },
        new Customer { Id = 2, TenantId = tenant, OrganizationName = "Contoso Supplies", BillingEmail = "billing@contoso.test", Domain = "contoso.test", Type = CustomerType.Vendor }
    );

    db.InvoiceNumberSequences.Add(new InvoiceNumberSequence { TenantId = tenant, NextValue = 1 });

    var inv = new Invoice { Id = 1001, TenantId = tenant, CustomerId = 1, Currency = "USD" };
    inv.Items.Add(new InvoiceItem { Id = 1, InvoiceId = inv.Id, Description = "Azure compute hours", Quantity = 10, UnitPrice = 12 });
    inv.Items.Add(new InvoiceItem { Id = 2, InvoiceId = inv.Id, Description = "Support plan", Quantity = 1, UnitPrice = 33 });
    inv.RecalculateTotals();
    db.Invoices.Add(inv);

    await db.SaveChangesAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
