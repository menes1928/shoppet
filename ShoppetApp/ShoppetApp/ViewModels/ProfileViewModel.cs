using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ShoppetApp.Helpers;
using ShoppetApp.Messages;
using ShoppetApp.Services;
using ContactModel = ShoppetApp.Models.Contact;

namespace ShoppetApp.ViewModels
{
    public partial class ProfileViewModel : ObservableObject, IRecipient<DataChangedMessage>
    {
        private readonly ApiService _api;
        private readonly DatabaseService _db;

        [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<ContactModel> _contacts = [];
        private System.Collections.Generic.List<ContactModel> _allContacts = new();

        [ObservableProperty]
        private string _contactSearchQuery;

        partial void OnContactSearchQueryChanged(string value)
        {
            FilterContacts();
        }

        private void FilterContacts()
        {
            if (string.IsNullOrWhiteSpace(ContactSearchQuery))
            {
                Contacts = new System.Collections.ObjectModel.ObservableCollection<ContactModel>(_allContacts);
            }
            else
            {
                var q = ContactSearchQuery.ToLowerInvariant();
                var filtered = _allContacts.Where(c => (c.Name?.ToLowerInvariant().Contains(q) == true) || (c.Role?.ToLowerInvariant().Contains(q) == true));
                Contacts = new System.Collections.ObjectModel.ObservableCollection<ContactModel>(filtered);
            }
        }

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _fullName = string.Empty;

        // Ensure these attributes are present so the compiler generates IsAdmin and IsBusinessOwner
        [ObservableProperty] private bool _isAdmin;
        [ObservableProperty] private bool _isBusinessOwner;

        public ProfileViewModel(ApiService api, DatabaseService db)
        {
            _api = api;
            _db = db;
            WeakReferenceMessenger.Default.Register(this);
        }

        public void Receive(DataChangedMessage message) =>
            MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                int savedUserId = Preferences.Get("LoggedInUserId", 0);
                if (savedUserId > 0)
                {
                    FullName = Preferences.Get("LoggedInFullName", "");
                    string role = Preferences.Get("LoggedInRole", "User");
                    IsAdmin = role == "Admin";
                    IsBusinessOwner = role == "BusinessOwner";
                }
                var contacts = await _api.GetContactsAsync();
                _allContacts = contacts.ToList(); FilterContacts();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddContactAsync() =>
            await Shell.Current.GoToAsync("contactform");

        [RelayCommand]
        private async Task ViewContactDetailsAsync(ContactModel contact)
        {
            if (contact is null) return;
            var popup = new Controls.ContactDetailsPopup(contact);
            await Shell.Current.Navigation.PushModalAsync(popup);
        }

        [RelayCommand]
        private async Task EditContactAsync(ContactModel contact) =>
            await Shell.Current.GoToAsync($"contactform?contactId={contact.Id}");

        [RelayCommand]
        private async Task CallContactAsync(ContactModel contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Phone))
                return;

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(contact.Phone);
                else
                    await Launcher.Default.OpenAsync($"tel:{contact.Phone}");
            }
            catch
            {
                await Shell.Current.DisplayAlert("Phone", "Unable to open dialer.", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteContactAsync(ContactModel contact)
        {
            bool confirm = await Shell.Current.DisplayAlert("Delete", $"Are you sure you want to delete {contact.Name}?", "Yes", "No");
            if (!confirm)
                return;

            await _api.DeleteContactAsync(contact.Id);
            WeakReferenceMessenger.Default.Send(DataChangedMessage.Instance);
            await LoadAsync();
        }

        [RelayCommand]
        private async Task OpenAdminPanelAsync() =>
            await Shell.Current.DisplayAlert("Admin", "Opening Platform Admin Console...", "OK");

        [RelayCommand]
        private async Task OpenBusinessPanelAsync() =>
            await Shell.Current.DisplayAlert("Business Owner", "Opening Clinic Counter & Inventory Desk...", "OK");

        [RelayCommand]
        private async Task OpenLocalShopsAsync() =>
            await Shell.Current.DisplayAlert("Directory", "Opening Local Pet Shops & Clinics around Lipa...", "OK");

        [RelayCommand]
        private void Logout() => NavigationHelper.GoToAuth();
    }
}







