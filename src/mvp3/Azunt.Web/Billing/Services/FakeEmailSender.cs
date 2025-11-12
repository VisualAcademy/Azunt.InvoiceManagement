using System;
using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public class FakeEmailSender : IEmailSender
{
    private readonly OutboxService _outbox;
    public FakeEmailSender(OutboxService outbox) { _outbox = outbox; }

    public Task<bool> SendInvoiceEmailAsync(Invoice invoice, Customer customer, byte[] pdfBytes, string viewLink)
    {
        var html = $@"
          <p>Sign in to view your <b>{customer.OrganizationName}</b> invoice.</p>
          <p><a href=""{viewLink}"">View your invoice &gt;</a></p>
          <p>If you’ve already paid, please disregard this email.</p>
          <hr />
          <p><b>Account information</b><br/>
             Organization name: {customer.OrganizationName}<br/>
             Domain: {customer.Domain}
          </p>
          <p><a href=""{viewLink}"">Download PDF</a></p>";

        _outbox.Emails.Add(new OutboxService.OutboxMail
        {
            To = customer.BillingEmail,
            Subject = $"Your invoice {invoice.InvoiceNumber}",
            Html = html,
            Attachments = { new OutboxService.Attachment { FileName = $"{invoice.InvoiceNumber}.pdf", PublicUrl = viewLink } }
        });

        invoice.EmailSentUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
