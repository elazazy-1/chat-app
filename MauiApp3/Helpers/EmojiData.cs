namespace MauiApp3.Helpers;

public static class EmojiData
{
    public static readonly string[] Smileys =
    [
        "\U0001F600", "\U0001F603", "\U0001F604", "\U0001F601", "\U0001F606",
        "\U0001F605", "\U0001F602", "\U0001F923", "\U0001F60A", "\U0001F607",
        "\U0001F642", "\U0001F643", "\U0001F609", "\U0001F60C", "\U0001F60D",
        "\U0001F970", "\U0001F618", "\U0001F617", "\U0001F619", "\U0001F61A",
        "\U0001F60B", "\U0001F61B", "\U0001F61C", "\U0001F92A", "\U0001F61D",
        "\U0001F911", "\U0001F917", "\U0001F92D", "\U0001F92B", "\U0001F914",
        "\U0001F910", "\U0001F928", "\U0001F610", "\U0001F611", "\U0001F636",
        "\U0001F60F", "\U0001F612", "\U0001F644", "\U0001F62C", "\U0001F925",
        "\U0001F60C", "\U0001F614", "\U0001F62A", "\U0001F924", "\U0001F634",
        "\U0001F637", "\U0001F912", "\U0001F915", "\U0001F922", "\U0001F92E"
    ];

    public static readonly string[] Gestures =
    [
        "\U0001F44D", "\U0001F44E", "\U0001F44A", "\u270C\uFE0F", "\U0001F91E",
        "\U0001F44C", "\U0001F44B", "\U0001F64F", "\U0001F4AA", "\u2764\uFE0F",
        "\U0001F494", "\U0001F495", "\U0001F4AF", "\U0001F4A5", "\U0001F525",
        "\u2B50", "\U0001F31F", "\U0001F389", "\U0001F38A", "\U0001F381"
    ];

    public static readonly string[] All = [.. Smileys, .. Gestures];
}
