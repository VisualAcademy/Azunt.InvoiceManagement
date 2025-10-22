using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public interface IEmailSender {
    Task<bool> SendInvoiceEmailAsync(Invoice invoice, Customer customer, byte[] pdfBytes, string viewLink);
}
