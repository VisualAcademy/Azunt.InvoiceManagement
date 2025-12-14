using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public interface IEmailSender
{
    Task<bool> SendInvoiceEmailAsync(Invoice invoice, Client client, byte[] pdfBytes, string viewLink);
    Task<bool> SendInvoiceEmailAsync(Invoice invoice, string toEmail, string displayName, byte[] pdfBytes, string viewLink);
}
