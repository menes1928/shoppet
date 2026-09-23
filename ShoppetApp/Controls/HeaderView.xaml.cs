using ShoppetApp.Services;

namespace ShoppetApp.Controls;

public partial class HeaderView : ContentView
{
    public Services.CartService CartService { get; }

    public HeaderView()
    {
        InitializeComponent();
        
        // Resolve CartService from DI manually since we are in a control
#if WINDOWS
        CartService = MauiWinUIApplication.Current.Services.GetService<Services.CartService>();
#elif ANDROID
        CartService = MauiApplication.Current.Services.GetService<Services.CartService>();
#elif IOS || MACCATALYST
        CartService = MauiUIApplicationDelegate.Current.Services.GetService<Services.CartService>();
#else
        CartService = null;
#endif
        BindingContext = this;
    }

    private async void OnCartClicked(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("cart");
    }
}
