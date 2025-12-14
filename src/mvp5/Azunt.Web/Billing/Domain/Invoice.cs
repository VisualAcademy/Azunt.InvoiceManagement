using System;
using System.Collections.Generic;
using System.Linq;

namespace Azunt.Web.Billing.Domain;
public class Invoice
{
    public long Id { get; set; }
    // Legacy/demo linkage (kept for backward compatibility)
    public string TenantId { get; set; } = default!;
    public long? ClientId { get; set; }

    // New "single source" invoice header fields.
    // Invoices are now identified by (TenantName + Email) across the system.
    public string TenantName { get; set; } = string.Empty;
    public string TenantKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailNormalized { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? ClientName { get; set; }
    public string? ClientType { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime IssueDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Currency { get; set; } = "USD";
    public bool ApplyTax { get; set; } = false;
    public decimal TaxRate { get; set; } = 0m;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? PdfPath { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EmailSentUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }
    public Client? Client { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();
    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s!.Trim())).Trim();
    public void RecalculateTotals(decimal? taxRate = null)
    {
        Subtotal = Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice, 2));
        var rate = taxRate ?? TaxRate;
        if (!ApplyTax || rate <= 0) { Tax = 0; }
        else { Tax = Math.Round(Subtotal * rate, 2); }
        Total = Subtotal + Tax;
    }
}
