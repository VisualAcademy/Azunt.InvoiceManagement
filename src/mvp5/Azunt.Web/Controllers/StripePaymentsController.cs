using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azunt.Web.Billing.Data;
using Azunt.Web.Billing.Domain;
using Azunt.Web.Billing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace Azunt.Web.Controllers
{
    /// <summary>
    /// Handles Stripe Checkout payments for invoices in the Billing module.
    /// </summary>
    [Route("payments")]
    public class StripePaymentsController : Controller
    {
        private readonly BillingDbContext _db;
        private readonly IInvoiceService _invoices;

        public StripePaymentsController(BillingDbContext db, IInvoiceService invoices)
        {
            _db = db;
            _invoices = invoices;
        }

        /// <summary>
        /// Creates a Stripe Checkout Session for the given invoice and redirects to Stripe-hosted payment page.
        /// </summary>
        /// <param name="invoiceId">Invoice primary key.</param>
        [HttpGet("stripe-checkout/{invoiceId:long}")]
        public async Task<IActionResult> CreateStripeCheckoutSession(long invoiceId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted);

            if (invoice is null)
            {
                return NotFound();
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                // Already paid – just send back to the portal page.
                return Redirect($"/portal/pay/{invoiceId}");
            }

            if (invoice.Total <= 0)
            {
                return BadRequest("Invoice total must be greater than zero.");
            }

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{domain}/payments/stripe-success?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/payments/stripe-cancel?invoiceId={invoiceId}",
                ClientReferenceId = invoice.Id.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    ["InvoiceId"] = invoice.Id.ToString(),
                    ["TenantId"] = invoice.TenantId
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = (invoice.Currency ?? "USD").ToLowerInvariant(),
                            // Stripe expects the smallest currency unit (e.g. cents for USD)
                            UnitAmount = (long)Math.Round(invoice.Total * 100m, 0),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = string.IsNullOrEmpty(invoice.InvoiceNumber)
                                    ? $"Invoice #{invoice.Id}"
                                    : invoice.InvoiceNumber,
                                Description = invoice.Client?.OrganizationName
                            }
                        }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Redirect the browser to Stripe-hosted Checkout page
            return Redirect(session.Url);
        }

        /// <summary>
        /// Success return URL from Stripe Checkout.
        /// Marks the invoice as Paid (demo level – for production use webhooks).
        /// </summary>
        [HttpGet("stripe-success")]
        public async Task<IActionResult> StripeSuccess(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Redirect("/portal");
            }

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("InvoiceId", out var invoiceIdValue) ||
                !long.TryParse(invoiceIdValue, out var invoiceId))
            {
                return Redirect("/portal");
            }

            // Basic safety: only mark as paid when Stripe reports payment_status = "paid"
            if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                await _invoices.MarkPaidAsync(invoiceId);
            }

            // Redirect back to the portal "Pay Now" page so the user can see the updated status
            return Redirect($"/portal/pay/{invoiceId}?paid=1");
        }

        /// <summary>
        /// Cancel return URL from Stripe Checkout.
        /// Only redirects back to the portal so the user can choose another method or retry.
        /// </summary>
        [HttpGet("stripe-cancel")]
        public IActionResult StripeCancel(long invoiceId)
        {
            return Redirect($"/portal/pay/{invoiceId}?canceled=1");
        }
    }
}
