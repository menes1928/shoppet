using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage() : this(App.Services.GetRequiredService<ProfileViewModel>()) { }

    public ProfilePage(ProfileViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeToAsync(1, 200);
        await _viewModel.LoadAsync();
    }
}
