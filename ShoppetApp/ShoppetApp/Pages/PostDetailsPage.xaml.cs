using ShoppetApp.ViewModels;

namespace ShoppetApp.Pages;

public partial class PostDetailsPage : ContentPage
{
    public PostDetailsPage(PostDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
