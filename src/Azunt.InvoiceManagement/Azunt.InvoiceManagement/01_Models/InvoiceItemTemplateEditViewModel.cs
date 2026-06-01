namespace Azunt.InvoiceManagement;

/// <summary>
/// Represents the create/edit form state for an invoice item template.
/// This view model is intended for Blazor modal forms, MVC view models, and API payloads.
/// </summary>
public class InvoiceItemTemplateEditViewModel
{
    /// <summary>
    /// Invoice item template unique identifier. A value of 0 means a new template.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Tenant scope identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Internal template name shown in management screens or selection lists.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invoice item description copied when the template is selected.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Default unit price copied to a new invoice item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Default quantity copied to a new invoice item.
    /// </summary>
    public decimal DefaultQuantity { get; set; } = 1m;

    /// <summary>
    /// Indicates whether this template is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order used when listing templates.
    /// </summary>
    public int SortOrder { get; set; }
}
