namespace Azunt.Web.Billing.Domain;
public class InvoiceNumberSequence
{
    public string TenantId { get; set; } = default!;
    public long NextValue { get; set; }
}
