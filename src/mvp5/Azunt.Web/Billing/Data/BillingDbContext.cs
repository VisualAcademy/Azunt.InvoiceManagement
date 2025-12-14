using Azunt.Web.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Azunt.Web.Billing.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantName).HasMaxLength(128);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.ClientType).HasMaxLength(64);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.InvoiceNumber).HasMaxLength(32);
            e.Property(x => x.ApplyTax);
            e.Property(x => x.TaxRate);
            e.Property(x => x.IsDeleted);
            e.Property(x => x.DeletedUtc);
            e.HasIndex(x => new { x.TenantId, x.InvoiceNumber }).IsUnique().HasFilter("[InvoiceNumber] IS NOT NULL AND [InvoiceNumber] <> ''");
            e.HasIndex(x => new { x.TenantName, x.Email });
            e.HasMany(x => x.Items).WithOne(i => i.Invoice!).HasForeignKey(i => i.InvoiceId);
        });
        b.Entity<InvoiceItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(200);
        });
        b.Entity<Client>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.OrganizationName).HasMaxLength(200);
            e.Property(x => x.BillingEmail).HasMaxLength(200);
            e.Property(x => x.Domain).HasMaxLength(200);
        });
        b.Entity<InvoiceNumberSequence>(e =>
        {
            e.HasKey(x => x.TenantId);
        });
    }
}
