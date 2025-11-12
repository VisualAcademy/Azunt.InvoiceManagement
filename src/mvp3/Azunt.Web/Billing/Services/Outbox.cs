using System;
using System.Collections.Generic;

namespace Azunt.Web.Billing.Services;
public class OutboxService
{
    public class OutboxMail
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
    public class Attachment
    {
        public string FileName { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
    }
    public List<OutboxMail> Emails { get; } = new();
}
