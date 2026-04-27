namespace MauiApp3.Features.Join;

/// <summary>
/// Code-behind for the Join page. Handles UI interactions before the user enters the lobby.
/// </summary>
public partial class JoinPage : ContentPage
{
    private readonly JoinViewModel _viewModel;

    public JoinPage(JoinViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        // Automatically trigger the join process when the user presses "Enter" on their soft keyboard
        if (_viewModel.JoinCommand.CanExecute(null))
            _viewModel.JoinCommand.Execute(null);
    }

    private void OnNameEntryFocused(object? sender, FocusEventArgs e)
    {
        // Intentionally left blank:
        // The AdjustResize window soft input mode handles keyboard overlap automatically
    }
}
