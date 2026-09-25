using ShoppetApp.Pages;

namespace ShoppetApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("petpassport", typeof(PetPassportPage));
        Routing.RegisterRoute("petform", typeof(PetFormPage));
        Routing.RegisterRoute("healthlogform", typeof(HealthLogFormPage));
        Routing.RegisterRoute("contactform", typeof(ContactFormPage));
        Routing.RegisterRoute("cart", typeof(CartPage));
        Routing.RegisterRoute("foodlogform", typeof(FoodLogFormPage));
        Routing.RegisterRoute("community", typeof(CommunityPage));
        Routing.RegisterRoute("CreatePostPage", typeof(CreatePostPage));
        Routing.RegisterRoute("PostDetailsPage", typeof(PostDetailsPage)); // Replace with your actual Community page namespace and class name
    }
}


