using System;
using System.Collections.Generic;

namespace webBackend.Models;

public partial class UserMessage
{
    public int MessageId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? MessageSubject { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}
