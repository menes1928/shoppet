using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class ContactFormPage : ContentPage, IQueryAttributable
{
    private readonly ContactFormViewModel _viewModel;

    public ContactFormPage() : this(App.Services.GetRequiredService<ContactFormViewModel>()) { }

    
    private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
            return;

        bool isValid = true;
        foreach (char c in e.NewTextValue)
        {
            if (!char.IsDigit(c))
            {
                isValid = false;
                break;
            }
        }

        if (!isValid)
        {
            Dispatcher.Dispatch(() => { ((Entry)sender).Text = e.OldTextValue; });
        }
    }

    public ContactFormPage(ContactFormViewModel viewModel)
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
