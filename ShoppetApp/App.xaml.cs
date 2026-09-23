namespace ShoppetApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider services)
    {
        Services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var splash = Services.GetRequiredService<Pages.SplashPage>();
        return new Window(new NavigationPage(splash)
        {
            BarBackgroundColor = Colors.Transparent,
            BarTextColor = Color.FromArgb("#4a7c82")
        });
    }
}
