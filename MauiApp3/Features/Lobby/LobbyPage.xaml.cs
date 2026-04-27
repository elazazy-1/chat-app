namespace MauiApp3.Features.Lobby;

/// <summary>
/// Code-behind for the Lobby page. Displays the list of peers and navigation to chats.
/// </summary>
public partial class LobbyPage : ContentPage
{
    public LobbyPage(LobbyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
