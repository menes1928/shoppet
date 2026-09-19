using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class HealthLogFormPage : ContentPage, IQueryAttributable
{
    private readonly HealthLogFormViewModel _viewModel;

    public HealthLogFormPage() : this(App.Services.GetRequiredService<HealthLogFormViewModel>()) { }

    public HealthLogFormPage(HealthLogFormViewModel viewModel)
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
