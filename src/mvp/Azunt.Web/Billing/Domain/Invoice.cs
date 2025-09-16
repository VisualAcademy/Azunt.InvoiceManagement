using System;
using System.Collections.Generic;
using System.Linq;

namespace Azunt.Web.Billing.Domain;
public class Invoice {
    public long Id { get; set; }
    public string TenantId { get; set; } = default!;
    public long CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? PdfPath { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EmailSentUtc { get; set; }
    public Customer? Customer { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();
    public void RecalculateTotals(decimal taxRate = 0.1m) {
        Subtotal = Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice, 2));
        Tax = Math.Round(Subtotal * taxRate, 2);
        Total = Subtotal + Tax;
    }
}
