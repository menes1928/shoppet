using ShoppetApp.Services;

namespace ShoppetApp.Pages;

public partial class ShopPage : ContentPage
{
    private readonly DatabaseService _db;
    private List<StoreDirectoryModel> _allStores = new();

    public ShopPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDirectory();
    }

    private void LoadDirectory()
    {
        // Fallback robust directory list tailored for Lipa & Tanauan local partner pet shops & hybrid clinics
        _allStores = new List<StoreDirectoryModel>
        {
            new StoreDirectoryModel
            {
                StoreName = "Maxivet Care Grooming and Veterinary Clinic",
                Location = "Bugtong na Pulo, Lipa / Tanauan Border",
                Hours = "Mon - Sun: 8:00 AM - 5:00 PM",
                Brands = "Royal Canin, NexGard Spectra, Pedigree, Aozi",
                MessengerLink = "https://www.facebook.com/BarkNPurrVet",
                StoreBadge = "🩺🛒 HYBRID CLINIC & SHOP"
            },
            new StoreDirectoryModel
            {
                StoreName = "Lipa Paw Spawt Pet Supplies",
                Location = "Ayala Highway, Lipa City",
                Hours = "Mon - Sat: 9:00 AM - 7:00 PM",
                Brands = "Royal Canin, Vitality, Top Breed, Beaphar",
                MessengerLink = "https://www.facebook.com",
                StoreBadge = "🛒 PET SUPPLY STORE"
            },
            new StoreDirectoryModel
            {
                StoreName = "Tanauan Animal Care & Hub",
                Location = "P. Torres St, Tanauan City",
                Hours = "Mon - Sat: 8:00 AM - 6:00 PM",
                Brands = "Special Dog, Monge, NexGard, Frontline",
                MessengerLink = "https://www.facebook.com",
                StoreBadge = "🩺🛒 HYBRID CLINIC & SHOP"
            }
        };

        ShopsListView.ItemsSource = _allStores;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            ShopsListView.ItemsSource = _allStores;
        }
        else
        {
            ShopsListView.ItemsSource = _allStores.Where(s =>
                s.StoreName.ToLower().Contains(query) ||
                s.Location.ToLower().Contains(query) ||
                s.Brands.ToLower().Contains(query)).ToList();
        }
    }

    private void OnFilterAllClicked(object sender, EventArgs e) => ShopsListView.ItemsSource = _allStores;

    private void OnFilterShopsClicked(object sender, EventArgs e) =>
        ShopsListView.ItemsSource = _allStores.Where(s => s.StoreBadge.Contains("PET SUPPLY STORE")).ToList();

    private void OnFilterHybridsClicked(object sender, EventArgs e) =>
        ShopsListView.ItemsSource = _allStores.Where(s => s.StoreBadge.Contains("HYBRID")).ToList();

    private async void OnMessageStoreClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string url && !string.IsNullOrEmpty(url))
        {
            try
            {
                await Launcher.OpenAsync(new Uri(url));
            }
            catch
            {
                await DisplayAlert("Error", "Could not open store link.", "OK");
            }
        }
    }
}

public class StoreDirectoryModel
{
    public string StoreName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Hours { get; set; } = string.Empty;
    public string Brands { get; set; } = string.Empty;
    public string MessengerLink { get; set; } = string.Empty;
    public string StoreBadge { get; set; } = string.Empty;
}