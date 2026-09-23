namespace ShoppetApp.Controls;

public partial class FloatingTabBar : ContentView
{
    public static readonly BindableProperty SelectedTabProperty =
        BindableProperty.Create(nameof(SelectedTab), typeof(string), typeof(FloatingTabBar), "home",
            propertyChanged: OnSelectedTabChanged);

    public string SelectedTab
    {
        get => (string)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public FloatingTabBar()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateTabStyles();
        if (Shell.Current is not null)
            Shell.Current.Navigated += OnShellNavigated;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var route = Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        if (route.Contains("community", StringComparison.OrdinalIgnoreCase))
            SelectedTab = "community";
        else if (route.Contains("pets", StringComparison.OrdinalIgnoreCase))
            SelectedTab = "pets";
        else if (route.Contains("shop", StringComparison.OrdinalIgnoreCase))
            SelectedTab = "shop";
        else if (route.Contains("profile", StringComparison.OrdinalIgnoreCase))
            SelectedTab = "profile";
        else if (route.Contains("home", StringComparison.OrdinalIgnoreCase))
            SelectedTab = "home";
    }

    private static void OnSelectedTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingTabBar bar)
            bar.UpdateTabStyles();
    }

    private void UpdateTabStyles()
    {
        StyleTab(HomeContainer, HomeIcon, HomeLabel, SelectedTab == "home");
        StyleTab(ShopContainer, ShopIcon, ShopLabel, SelectedTab == "shop");
        StyleTab(PetsContainer, PetsIcon, PetsLabel, SelectedTab == "pets");
        StyleTab(CommunityContainer, CommunityIcon, CommunityLabel, SelectedTab == "community");
        StyleTab(ProfileContainer, ProfileIcon, ProfileLabel, SelectedTab == "profile");
    }

    private static void StyleTab(Border container, Microsoft.Maui.Controls.Shapes.Path icon, Label label, bool isActive)
    {
        var resources = Application.Current!.Resources;
        var primary = (Color)resources["Primary"];
        var muted = (Color)resources["NavMuted"];
        var onPrimary = (Color)resources["PrimaryFg"];

        container.BackgroundColor = isActive ? primary : Colors.Transparent;
        icon.Fill = new SolidColorBrush(isActive ? onPrimary : muted);
        label.TextColor = isActive ? onPrimary : muted;
        label.FontFamily = isActive ? "NunitoExtraBold" : "NunitoSemiBold";
        container.Scale = 1;
    }

    private async void OnHomeClicked(object? sender, TappedEventArgs e)
    {
        await BounceAsync(HomeContainer);
        SelectedTab = "home";
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnShopClicked(object? sender, TappedEventArgs e)
    {
        await BounceAsync(ShopContainer);
        SelectedTab = "shop";
        await Shell.Current.GoToAsync("//shop");
    }

    private async void OnPetsClicked(object? sender, TappedEventArgs e)
    {
        await BounceAsync(PetsContainer);
        SelectedTab = "pets";
        await Shell.Current.GoToAsync("//pets");
    }

    private async void OnCommunityClicked(object? sender, TappedEventArgs e)
    {
        await BounceAsync(CommunityContainer);
        SelectedTab = "community";
        await Shell.Current.GoToAsync("//community");
    }

    private async void OnProfileClicked(object? sender, TappedEventArgs e)
    {
        await BounceAsync(ProfileContainer);
        SelectedTab = "profile";
        await Shell.Current.GoToAsync("//profile");
    }

    private static async Task BounceAsync(VisualElement view)
    {
        await view.ScaleToAsync(0.94, 80, Easing.SinOut);
        await view.ScaleToAsync(1, 120, Easing.SinIn);
    }
}