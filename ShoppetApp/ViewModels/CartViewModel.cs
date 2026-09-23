using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppetApp.Models;
using ShoppetApp.Services;

namespace ShoppetApp.ViewModels;

public partial class CartViewModel : ObservableObject
{
    public CartService CartService { get; }
    private readonly DatabaseService _db;

    public CartViewModel(CartService cartService, DatabaseService db)
    {
        CartService = cartService;
        _db = db;
    }

    [RelayCommand]
    private async Task IncreaseQuantity(CartItem item)
    {
        if (item != null)
        {
            await CartService.UpdateCartItemAsync(item.Id, item.Quantity + 1);
        }
    }

    [RelayCommand]
    private async Task DecreaseQuantity(CartItem item)
    {
        if (item != null && item.Quantity > 1)
        {
            await CartService.UpdateCartItemAsync(item.Id, item.Quantity - 1);
        }
        else if (item != null)
        {
            await CartService.RemoveFromCartAsync(item.Id);
        }
    }

    [RelayCommand]
    private async Task RemoveItem(CartItem item)
    {
        if (item != null)
        {
            await CartService.RemoveFromCartAsync(item.Id);
        }
    }

    [RelayCommand]
    private async Task Checkout()
    {
        var items = await CartService.GetCartAsync();
        if (items == null || items.Count == 0)
        {
            await Shell.Current.DisplayAlert("Cart Empty", "Add some items to your cart first.", "OK");
            return;
        }

        decimal totalAmount = items.Sum(i => i.Price * i.Quantity);
        bool success = await CartService.CheckoutAsync();

        if (success)
        {
            await Shell.Current.DisplayAlert("Checkout", $"Your total is ${totalAmount:F2}. Order placed successfully!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert("Checkout Failed", "There was an error placing your order.", "OK");
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}