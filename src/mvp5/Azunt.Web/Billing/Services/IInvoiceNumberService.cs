using System.Threading.Tasks;

namespace Azunt.Web.Billing.Services;
public interface IInvoiceNumberService
{
    Task<string> GetNextInvoiceNumberAsync(string tenantId);
}
