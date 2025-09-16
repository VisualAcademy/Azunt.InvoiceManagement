using System;
using System.Threading.Tasks;
using Azunt.Web.Billing.Domain;

namespace Azunt.Web.Billing.Services;
public interface IInvoiceService {
    Task<Invoice> GetAsync(long id);
    Task<Invoice> CreateDraftAsync(string tenantId, long customerId, string currency = "USD");
    Task AddItemAsync(long invoiceId, string description, decimal qty, decimal unitPrice);
    Task IssueAsync(long invoiceId);
    Task MarkSentAsync(long invoiceId);
    Task MarkPaidAsync(long invoiceId);
    Task UpdateItemAsync(long invoiceId, long itemId, string description, decimal qty, decimal unitPrice);
    Task RemoveItemAsync(long invoiceId, long itemId);
    Task UpdateInvoiceInfoAsync(long invoiceId, DateTime? dueDateUtc, long? customerId = null);
    Task<Invoice> CloneAsDraftAsync(long invoiceId);
}
