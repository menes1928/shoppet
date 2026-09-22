using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppetApp.Models;
using ShoppetApp.Services;

namespace ShoppetApp.ViewModels
{
    public partial class CreatePostViewModel : ObservableObject
    {
        private readonly ApiService _api;
        private readonly DatabaseService _db;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private ObservableCollection<object> _selectedPets = new();

        [ObservableProperty]
        private string _attachedPhotoPath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Pet> _myPets = new();

        [ObservableProperty]
        private bool _isPetModalVisible;

        public string UserFullName => _db.CurrentUser?.FullName ?? "User";
        public string UserInitials => string.IsNullOrWhiteSpace(UserFullName) ? "U" : UserFullName.Substring(0, 1).ToUpper();

        public CreatePostViewModel(ApiService api, DatabaseService db)
        {
            _api = api;
            _db = db;
        }

        public async Task LoadPetsAsync()
        {
            try
            {
                var pets = await _api.GetPetsAsync();
                MyPets.Clear();
                foreach(var p in pets)
                {
                    MyPets.Add(p);
                }
            }
            catch { }
        }

        [RelayCommand]
        private void OpenPetModal()
        {
            IsPetModalVisible = true;
        }

        [RelayCommand]
        private void ClosePetModal()
        {
            IsPetModalVisible = false;
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task AttachPhotoAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a Photo",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    AttachedPhotoPath = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [RelayCommand]
        private async Task PostAsync()
        {
            if (string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(AttachedPhotoPath))
                return;

            if (_db.CurrentUser == null) return;

            var request = new 
            {
                UserId = _db.CurrentUser.Id,
                PetId = SelectedPets.FirstOrDefault() is Pet firstPet ? (int?)firstPet.Id : null,
                AuthorName = _db.CurrentUser.FullName,
                PetName = SelectedPets.Count > 0 ? string.Join(" and ", SelectedPets.Cast<Pet>().Select(p => p.Name)) : "",
                Content = Content,
                ImageUrls = AttachedPhotoPath // Simple string for now
            };

            var success = await _api.CreateCommunityPostAsync(request);
            if (success)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to post story. Try again.", "OK");
            }
        }
    }
}







