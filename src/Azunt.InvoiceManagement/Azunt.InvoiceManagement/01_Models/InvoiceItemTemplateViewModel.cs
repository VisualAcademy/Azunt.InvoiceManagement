namespace Azunt.InvoiceManagement;

/// <summary>
/// Represents an invoice item template row for list and detail display.
/// This view model is aligned with the main columns of the InvoiceItemTemplates table.
/// </summary>
public class InvoiceItemTemplateViewModel
{
    /// <summary>
    /// Invoice item template unique identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Tenant scope identifier. Empty or null-like values can be used by host applications for global templates.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant key for systems that distinguish TenantId from a tenant code/key.
    /// </summary>
    public string TenantKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant display name.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Internal template name shown in management screens or selection lists.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invoice item description copied when the template is selected.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Default quantity copied to a new invoice item.
    /// </summary>
    public decimal DefaultQuantity { get; set; } = 1m;

    /// <summary>
    /// Default unit price copied to a new invoice item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Optional currency code. Host applications may use the invoice currency when this value is empty.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this template is taxable.
    /// </summary>
    public bool IsTaxable { get; set; }

    /// <summary>
    /// Indicates whether this template is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order used when listing templates.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates whether this template has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the template was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the template was last updated.
    /// </summary>
    public DateTime UpdatedUtc { get; set; }
}
