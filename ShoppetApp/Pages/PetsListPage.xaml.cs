using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class PetsListPage : ContentPage
{
    private readonly PetsListViewModel _viewModel;

    public PetsListPage() : this(App.Services.GetRequiredService<PetsListViewModel>()) { }

    public PetsListPage(PetsListViewModel viewModel)
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
