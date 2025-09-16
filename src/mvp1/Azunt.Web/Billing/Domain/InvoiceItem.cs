namespace Azunt.Web.Billing.Domain;
public class InvoiceItem {
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Invoice? Invoice { get; set; }
}
