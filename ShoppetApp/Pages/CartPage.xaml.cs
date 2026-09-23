using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class CartPage : ContentPage
{
    public CartPage(CartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CartViewModel vm)
        {
            await vm.CartService.LoadCartAsync();
        }
    }
}
