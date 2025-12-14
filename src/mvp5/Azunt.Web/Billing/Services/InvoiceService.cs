using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Azunt.Web.Billing.Data;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public class InvoiceService : IInvoiceService
{
    private readonly BillingDbContext _db;
    private readonly IInvoiceNumberService _num;

    public InvoiceService(BillingDbContext db, IInvoiceNumberService num)
    {
        _db = db;
        _num = num;
    }

    public Task<Invoice> GetAsync(long id) =>
        _db.Invoices.Include(i => i.Items).Include(i => i.Client)
            .FirstAsync(i => i.Id == id);

    public async Task<Invoice> CreateDraftAsync(string tenantId, long clientId, string currency = "USD")
    {
        var inv = new Invoice
        {
            TenantId = tenantId,
            ClientId = clientId,
            TenantName = tenantId,
            TenantKey = tenantId,
            Email = string.Empty,
            EmailNormalized = string.Empty,
            FirstName = null,
            MiddleName = null,
            LastName = null,
            ClientName = null,
            ClientType = null,
            Currency = currency,
            Status = InvoiceStatus.Draft
        };
        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();
        return inv;
    }

    public async Task<Invoice> CreateDraftAsync(string tenantName, string email, string? clientName, string? clientType, string? firstName, string? middleName, string? lastName, string currency = "USD")
{
    var tn = (tenantName ?? string.Empty).Trim();
    var em = (email ?? string.Empty).Trim().ToLowerInvariant();
    var cn = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();
    var ct = string.IsNullOrWhiteSpace(clientType) ? null : clientType.Trim();

    var fn = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
    var mn = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
    var ln = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();

    var inv = new Invoice
    {
        // We keep TenantId for existing invoice number sequencing.
        TenantId = tn,
        ClientId = null,
        TenantName = tn,
        TenantKey = tn,
        Email = em,
        EmailNormalized = em,
        FirstName = fn,
        MiddleName = mn,
        LastName = ln,
        ClientName = cn,
        ClientType = ct,
        Currency = currency,
        Status = InvoiceStatus.Draft
    };

    _db.Invoices.Add(inv);
    await _db.SaveChangesAsync();
    return inv;
}

    public async Task AddItemAsync(long invoiceId, string description, decimal qty, decimal unitPrice)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be modified.");
        var item = new InvoiceItem { InvoiceId = invoiceId, Description = description, Quantity = qty, UnitPrice = unitPrice };
        _db.InvoiceItems.Add(item);
        inv.RecalculateTotals();
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task IssueAsync(long invoiceId)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft can be issued.");
        var tenantKey = !string.IsNullOrWhiteSpace(inv.TenantKey) ? inv.TenantKey : (!string.IsNullOrWhiteSpace(inv.TenantName) ? inv.TenantName : inv.TenantId);
        inv.InvoiceNumber = await _num.GetNextInvoiceNumberAsync(tenantKey);
        inv.IssueDateUtc = DateTime.UtcNow;
        inv.RecalculateTotals();
        inv.Status = InvoiceStatus.Issued;
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkSentAsync(long invoiceId)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status is not InvoiceStatus.Issued and not InvoiceStatus.Sent)
            throw new InvalidOperationException("Only Issued can be marked as Sent.");
        inv.Status = InvoiceStatus.Sent;
        inv.EmailSentUtc = DateTime.UtcNow;
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkPaidAsync(long invoiceId)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Sent)
            throw new InvalidOperationException("Only Sent can be marked as Paid.");
        inv.Status = InvoiceStatus.Paid;
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(long invoiceId, long itemId, string description, decimal qty, decimal unitPrice)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be modified.");
        var item = inv.Items.FirstOrDefault(x => x.Id == itemId) ?? throw new InvalidOperationException("Item not found.");
        item.Description = description;
        item.Quantity = qty;
        item.UnitPrice = unitPrice;
        inv.RecalculateTotals();
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(long invoiceId, long itemId)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be modified.");
        var item = inv.Items.FirstOrDefault(x => x.Id == itemId);
        if (item is null) return;
        _db.InvoiceItems.Remove(item);
        inv.Items.Remove(item);
        inv.RecalculateTotals();
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateInvoiceInfoAsync(long invoiceId, DateTime? dueDateUtc, long? clientId = null)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be modified.");
        inv.DueDateUtc = dueDateUtc;
        if (clientId is long cid && cid != 0 && cid != (inv.ClientId ?? 0))
        {
            if (!await _db.Clients.AnyAsync(c => c.Id == cid))
                throw new InvalidOperationException("Client not found.");
            inv.ClientId = cid;
        }
        inv.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<Invoice> CloneAsDraftAsync(long invoiceId)
    {
        var src = await GetAsync(invoiceId);
        var copy = new Invoice
        {
            TenantId = src.TenantId,
            ClientId = src.ClientId,
            TenantName = src.TenantName,
            Email = src.Email,
            ClientName = src.ClientName,
            ClientType = src.ClientType,
            Currency = src.Currency,
            Status = InvoiceStatus.Draft
        };
        foreach (var it in src.Items)
        {
            copy.Items.Add(new InvoiceItem { Description = it.Description, Quantity = it.Quantity, UnitPrice = it.UnitPrice });
        }
        copy.RecalculateTotals();
        _db.Invoices.Add(copy);
        await _db.SaveChangesAsync();
        return copy;
    }

    public async Task SoftDeleteAsync(long id)
    {
        var inv = await _db.Invoices.FirstAsync(i => i.Id == id);
        if (!inv.IsDeleted)
        {
            inv.IsDeleted = true;
            inv.DeletedUtc = DateTime.UtcNow;
            inv.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RestoreAsync(long id)
    {
        var inv = await _db.Invoices.FirstAsync(i => i.Id == id);
        if (inv.IsDeleted)
        {
            inv.IsDeleted = false;
            inv.DeletedUtc = null;
            inv.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}