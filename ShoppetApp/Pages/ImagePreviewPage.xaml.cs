namespace ShoppetApp.Pages;

[QueryProperty(nameof(ImageUrl), "ImageUrl")]
public partial class ImagePreviewPage : ContentPage
{
    private string? _imageUrl;
    public string? ImageUrl
    {
        get => _imageUrl;
        set
        {
            _imageUrl = value;
            PreviewImage.Source = value;
        }
    }

    public ImagePreviewPage()
    {
        InitializeComponent();
    }

    private async void OnCloseTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
