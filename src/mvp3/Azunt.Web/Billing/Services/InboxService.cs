
using System;
using System.Collections.Generic;
using System.Linq;

namespace Azunt.Web.Billing.Services;
public class InboxService
{
    private readonly OutboxService _outbox;
    public InboxService(OutboxService outbox) { _outbox = outbox; }

    public record InboxItem(string Email, string FileName, string PublicUrl, string Source, DateTime CreatedUtc);

    /// <summary>
    /// Returns all PDF-like attachments that were sent to the given email (case-insensitive).
    /// Source is "Email" for now; future options: "Vendor", "CandidatePortal".
    /// </summary>
    public List<InboxItem> GetItems(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return new();
        var target = email.Trim();
        return _outbox.Emails
            .Where(m => string.Equals(m.To, target, StringComparison.OrdinalIgnoreCase))
            .SelectMany(m => m.Attachments.Select(a => new InboxItem(m.To, a.FileName, a.PublicUrl, "Email", m.CreatedUtc)))
            .OrderByDescending(i => i.CreatedUtc)
            .ToList();
    }
}
