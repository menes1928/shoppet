using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class FoodLogFormPage : ContentPage, IQueryAttributable
{
    private readonly FoodLogFormViewModel _viewModel;

    public FoodLogFormPage() : this(App.Services.GetRequiredService<FoodLogFormViewModel>()) { }

    public FoodLogFormPage(FoodLogFormViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _viewModel.ApplyQueryAttributes(query);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        TranslationY = 400;
        await this.TranslateToAsync(0, 0, 300, Easing.SinOut);
        await _viewModel.LoadAsync();
    }
}
