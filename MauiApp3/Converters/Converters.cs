using System.Globalization;
using MauiApp3.Models;

namespace MauiApp3.Converters;

/// <summary>
/// Helper class to retrieve colors defined in application resources.
/// </summary>
internal static class ResourceColor
{
    public static Color Get(string key, Color fallback)
    {
        if (Application.Current?.Resources?.TryGetValue(key, out var value) == true)
        {
            if (value is Color color)
                return color;
            if (value is SolidColorBrush brush)
                return brush.Color;
        }

        return fallback;
    }
}

/// <summary>
/// Converter that determines the horizontal alignment of a message bubble
/// (e.g., right for my messages, left for others).
/// </summary>
public class MessageAlignmentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return LayoutOptions.Start;

        if (msg.MessageType == MessageType.System)
            return LayoutOptions.Center;

        return msg.IsMine ? LayoutOptions.End : LayoutOptions.Start;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that determines the background color of a message bubble.
/// </summary>
public class MessageBubbleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return Colors.White;

        if (msg.MessageType == MessageType.System)
            return ResourceColor.Get("ChatSystemMessage", Color.FromArgb("#E2E8F0"));

        var sent = ResourceColor.Get("ChatBubbleSent", Color.FromArgb("#E0F2FE"));
        var received = ResourceColor.Get("ChatBubbleReceived", Colors.White);
        return msg.IsMine ? sent : received;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns a green color for true (e.g., online status) 
/// and a default grey color for false.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return ResourceColor.Get("OnlineDot", Color.FromArgb("#22C55E"));
        return ResourceColor.Get("AppDivider", Color.FromArgb("#E2E8F0"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the message is a system message.
/// </summary>
public class SystemMessageVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return true;
        return msg.MessageType == MessageType.System;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the message is NOT a system message.
/// </summary>
public class NotSystemMessageVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return true;
        return msg.MessageType != MessageType.System;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true to show the sender's name for incoming messages.
/// Hides the sender's name for the current user's own messages and system messages.
/// </summary>
public class SenderVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return false;
        return !msg.IsMine && msg.MessageType != MessageType.System;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the message type is File.
/// </summary>
public class IsFileMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return false;
        return msg.MessageType == MessageType.File;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the message type is Audio.
/// </summary>
public class IsAudioMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return false;
        return msg.MessageType == MessageType.Audio;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the message type is Text.
/// </summary>
public class IsTextMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChatMessage msg) return true;
        return msg.MessageType == MessageType.Text;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that formats a DateTime into a short time string (e.g., "3:45 PM").
/// </summary>
public class TimestampConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToString("h:mm tt");
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns true if the unread message count is greater than zero.
/// Used to show/hide the unread badge.
/// </summary>
public class UnreadBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int count && count > 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>
/// Converter that returns the inverse of a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;
}
