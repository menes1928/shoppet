using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class CreatePostPage : ContentPage
{
    private readonly CreatePostViewModel _vm;

    public CreatePostPage(CreatePostViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadPetsAsync();
    }
}
