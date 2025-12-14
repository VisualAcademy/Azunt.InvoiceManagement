using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public interface IInvoicePdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Client client);
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, string billToName, string billToEmail, string? domain = null);
}
