using System.Text.Json.Serialization;

namespace MauiApp3.Models;

/// <summary>
/// Defines the different types of messages that can be sent in the chat.
/// </summary>
public enum MessageType
{
    /// <summary>A standard text message.</summary>
    Text,
    /// <summary>A message containing a file attachment.</summary>
    File,
    /// <summary>A message containing a voice/audio recording.</summary>
    Audio,
    /// <summary>A system generated message (e.g., user joined/left).</summary>
    System
}

/// <summary>
/// Represents a single message in the chat system.
/// </summary>
public class ChatMessage
{
    /// <summary>Unique identifier for the message.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Name of the user who sent the message.</summary>
    public string SenderName { get; set; } = string.Empty;
    
    /// <summary>IP address of the sender.</summary>
    public string SenderIP { get; set; } = string.Empty;
    
    /// <summary>The text content of the message.</summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>The type of this message (Text, File, Audio, System).</summary>
    public MessageType MessageType { get; set; } = MessageType.Text;
    
    /// <summary>The time the message was sent or created.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>Name of the attached file, if any.</summary>
    public string? FileName { get; set; }
    
    /// <summary>Base64 encoded representation of the attached file data.</summary>
    public string? FileDataBase64 { get; set; }
    
    /// <summary>Indicates whether this message is sent to the group or a specific user.</summary>
    public bool IsGroupMessage { get; set; } = true;
    
    /// <summary>The IP address of the target recipient for private messages.</summary>
    public string? TargetIP { get; set; }

    /// <summary>Indicates if the current user is the sender of this message.</summary>
    [JsonIgnore]
    public bool IsMine { get; set; }

    /// <summary>Helper property to get or set the raw file data as a byte array.</summary>
    [JsonIgnore]
    public byte[]? FileData
    {
        get => string.IsNullOrEmpty(FileDataBase64) ? null : Convert.FromBase64String(FileDataBase64);
        set => FileDataBase64 = value != null ? Convert.ToBase64String(value) : null;
    }
}
