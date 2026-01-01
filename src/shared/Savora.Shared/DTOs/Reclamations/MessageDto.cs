namespace Savora.Shared.DTOs.Reclamations;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderProfilePicture { get; set; }
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverProfilePicture { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class CreateMessageRequest
{
    public Guid ReceiverId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class MessageListDto
{
    public Guid Id { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserProfilePicture { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageDate { get; set; }
    public int UnreadCount { get; set; }
    public bool IsFromMe { get; set; }
}





