using System;
using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public class FakeEmailSender : IEmailSender
{
    private readonly OutboxService _outbox;
    public FakeEmailSender(OutboxService outbox) { _outbox = outbox; }

    public Task<bool> SendInvoiceEmailAsync(Invoice invoice, Client client, byte[] pdfBytes, string viewLink)
    {
        return SendInvoiceEmailAsync(invoice, client.BillingEmail, client.OrganizationName, pdfBytes, viewLink);
    }

    public Task<bool> SendInvoiceEmailAsync(Invoice invoice, string toEmail, string displayName, byte[] pdfBytes, string viewLink)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? (invoice.ClientName ?? "Client") : displayName;

        var html = $@"
          <p>Sign in to view your <b>{name}</b> invoice.</p>
          <p><a href=""{viewLink}"">View your invoice &gt;</a></p>
          <p>If you’ve already paid, please disregard this email.</p>
          <hr />
          <p><b>Account information</b><br/>
             Tenant: {invoice.TenantName}<br/>
             Name: {name}<br/>
             Email: {toEmail}
          </p>
          <p><a href=""{viewLink}"">Download PDF</a></p>";

        _outbox.Emails.Add(new OutboxService.OutboxMail
        {
            To = toEmail,
            Subject = $"Your invoice {(invoice.InvoiceNumber ?? invoice.Id.ToString())}",
            Html = html,
            Attachments = { new OutboxService.Attachment { FileName = $"{(invoice.InvoiceNumber ?? invoice.Id.ToString())}.pdf", PublicUrl = viewLink } }
        });

        invoice.EmailSentUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
