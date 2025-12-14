using System.IO;
using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Azunt.Web.Billing.Services;
public class QuestPdfInvoiceService : IInvoicePdfService
{
    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Client client)
    {
        return GenerateInvoicePdfAsync(invoice, client.OrganizationName, client.BillingEmail, client.Domain);
    }

    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, string billToName, string billToEmail, string? domain = null)
    {
        invoice.RecalculateTotals();

        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Margin(35);
                p.Header().Text($"INVOICE {invoice.InvoiceNumber}").SemiBold().FontSize(20);
                p.Content().Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(invoice.TenantName))
                        col.Item().Text($"Tenant: {invoice.TenantName}").SemiBold();

                    col.Item().Text($"Bill To: {billToName}");
                    if (!string.IsNullOrWhiteSpace(billToEmail))
                        col.Item().Text($"Email: {billToEmail}");
                    if (!string.IsNullOrWhiteSpace(domain))
                        col.Item().Text($"Domain: {domain}");

                    col.Item().Text($"Issue: {invoice.IssueDateUtc:yyyy-MM-dd}  Due: {invoice.DueDateUtc:yyyy-MM-dd}");
                    col.Item().LineHorizontal(1);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(x =>
                        {
                            x.RelativeColumn(6);
                            x.RelativeColumn(2);
                            x.RelativeColumn(2);
                            x.RelativeColumn(2);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().Text("Qty").Bold();
                            h.Cell().Text("Unit").Bold();
                            h.Cell().Text("Amount").Bold();
                        });
                        foreach (var it in invoice.Items)
                        {
                            t.Cell().Text(it.Description);
                            t.Cell().Text(it.Quantity.ToString("0.##"));
                            t.Cell().Text(it.UnitPrice.ToString("N2"));
                            t.Cell().Text((it.Quantity * it.UnitPrice).ToString("N2"));
                        }
                    });
                    col.Item().AlignRight().Text($"Subtotal: {invoice.Subtotal:N2}");
                    col.Item().AlignRight().Text($"Tax: {invoice.Tax:N2}");
                    col.Item().AlignRight().Text($"Total: {invoice.Total:N2} {invoice.Currency}").Bold();
                });
                p.Footer().AlignCenter().Text("Thank you for your business.");
            });
        });

        using var ms = new MemoryStream();
        doc.GeneratePdf(ms);
        return Task.FromResult(ms.ToArray());
    }
}
