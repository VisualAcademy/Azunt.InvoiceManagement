namespace Azunt.Web.Billing.Domain;

public class Customer
{
    public long Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrganizationName { get; set; } = default!;
    public string BillingEmail { get; set; } = default!;
    public string? Domain { get; set; }
    public CustomerType Type { get; set; } = CustomerType.Vendor;
}
