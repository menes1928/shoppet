using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class CommunityPage : ContentPage
{
    private readonly CommunityViewModel _vm;

    public CommunityPage(CommunityViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadPostsCommand.ExecuteAsync(null);
    }
}
