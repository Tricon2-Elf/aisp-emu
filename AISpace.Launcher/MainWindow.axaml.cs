using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AISpace.Launcher;

public partial class MainWindow : Window
{
    private readonly GameLauncher _gameLauncher;

    public MainWindow()
    {
        InitializeComponent();

        _gameLauncher = new GameLauncher(LauncherBootstrap.Settings);

        EnvironmentComboBox.SelectedIndex = Math.Clamp((int)LauncherBootstrap.Settings.SelectedEnvironment, 0, 2);
        WebsiteWebView.Source = new Uri(_gameLauncher.Settings.WebsiteUrl);
    }

    private async void OnStartGameClick(object? sender, RoutedEventArgs e)
    {
        var environment = (GameEnvironment)EnvironmentComboBox.SelectedIndex;
        LauncherBootstrap.Settings.SelectedEnvironment = environment;
        LauncherBootstrap.Settings.Save();

        var result = _gameLauncher.TryLaunch(environment);
        if (result.Succeeded)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
            return;
        }
        await ShowMessageAsync("Unable to start game", $"{result.Message}\n\n{result.Details}");
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80,
        };
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton,
                },
            },
        };

        okButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }
}
