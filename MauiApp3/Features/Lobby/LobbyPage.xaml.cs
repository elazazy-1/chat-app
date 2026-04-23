namespace MauiApp3.Features.Lobby;

public partial class LobbyPage : ContentPage
{
    public LobbyPage(LobbyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
