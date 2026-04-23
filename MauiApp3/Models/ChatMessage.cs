using System.Text.Json.Serialization;

namespace MauiApp3.Models;

public enum MessageType
{
    Text,
    File,
    Audio,
    System
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SenderName { get; set; } = string.Empty;
    public string SenderIP { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? FileName { get; set; }
    public string? FileDataBase64 { get; set; }
    public bool IsGroupMessage { get; set; } = true;
    public string? TargetIP { get; set; }

    [JsonIgnore]
    public bool IsMine { get; set; }

    [JsonIgnore]
    public byte[]? FileData
    {
        get => string.IsNullOrEmpty(FileDataBase64) ? null : Convert.FromBase64String(FileDataBase64);
        set => FileDataBase64 = value != null ? Convert.ToBase64String(value) : null;
    }
}
