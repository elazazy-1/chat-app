namespace MauiApp3.Features.Join;

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
        if (_viewModel.JoinCommand.CanExecute(null))
            _viewModel.JoinCommand.Execute(null);
    }

    private void OnNameEntryFocused(object? sender, FocusEventArgs e)
    {
        // AdjustResize handles keyboard visibility automatically
    }
}
