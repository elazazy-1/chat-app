using System.Globalization;
using MauiApp3.Models;

namespace MauiApp3.Converters;

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

public class UnreadBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int count && count > 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;
}
