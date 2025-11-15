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
        _db.Invoices.Include(i => i.Items).Include(i => i.Customer)
            .FirstAsync(i => i.Id == id);

    public async Task<Invoice> CreateDraftAsync(string tenantId, long customerId, string currency = "USD")
    {
        var inv = new Invoice
        {
            TenantId = tenantId,
            CustomerId = customerId,
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
        inv.InvoiceNumber = await _num.GetNextInvoiceNumberAsync(inv.TenantId);
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

    public async Task UpdateInvoiceInfoAsync(long invoiceId, DateTime? dueDateUtc, long? customerId = null)
    {
        var inv = await GetAsync(invoiceId);
        if (inv.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be modified.");
        inv.DueDateUtc = dueDateUtc;
        if (customerId is long cid && cid != 0 && cid != inv.CustomerId)
        {
            if (!await _db.Customers.AnyAsync(c => c.Id == cid))
                throw new InvalidOperationException("Customer not found.");
            inv.CustomerId = cid;
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
            CustomerId = src.CustomerId,
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