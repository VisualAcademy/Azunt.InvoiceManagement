using System;
using System.Threading.Tasks;
using Azunt.Web.Billing.Data;
using Azunt.Web.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Azunt.Web.Billing.Services;
public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly BillingDbContext _db;
    public InvoiceNumberService(BillingDbContext db) => _db = db;

    public async Task<string> GetNextInvoiceNumberAsync(string tenantId)
    {
        var seq = await _db.InvoiceNumberSequences.FindAsync(tenantId);
        if (seq is null)
        {
            seq = new InvoiceNumberSequence { TenantId = tenantId, NextValue = 1 };
            _db.InvoiceNumberSequences.Add(seq);
            await _db.SaveChangesAsync();
        }
        var next = seq.NextValue++;
        await _db.SaveChangesAsync();

        var y = DateTime.UtcNow.Year;
        return $"INV-{y}-{next:000000}";
    }
}
