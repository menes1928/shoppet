using ShoppetApp.Models;

namespace ShoppetApp.Controls;

public partial class ContactDetailsPopup : ContentPage
{
    public ContactDetailsPopup(ShoppetApp.Models.Contact contact)
    {
        InitializeComponent();
        BindingContext = contact;
    }

    private async void OnCloseClicked(object sender, EventArgs e) 
    {
        await Navigation.PopModalAsync();
    }
}
