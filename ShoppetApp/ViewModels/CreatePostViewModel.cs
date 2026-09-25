using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppetApp.Models;
using ShoppetApp.Services;

namespace ShoppetApp.ViewModels
{
    public partial class MediaAttachment : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private bool _isVideo;

        public bool IsImage => !IsVideo;
    }

    public partial class CreatePostViewModel : ObservableObject
    {
        private readonly ApiService _api;
        private readonly DatabaseService _db;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private ObservableCollection<object> _selectedPets = new();

        [ObservableProperty]
        private ObservableCollection<MediaAttachment> _attachedMedia = new();

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
        private void RemoveMedia(MediaAttachment media)
        {
            if (media != null && AttachedMedia.Contains(media))
            {
                AttachedMedia.Remove(media);
            }
        }

        [RelayCommand]
        private async Task AttachPhotoAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Select Photos",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    foreach (var file in result)
                    {
                        if (AttachedMedia.Count >= 5)
                        {
                            await Shell.Current.DisplayAlert("Limit Reached", "You can only attach a maximum of 5 photos.", "OK");
                            break;
                        }

                        AttachedMedia.Add(new MediaAttachment { FilePath = file.FullPath, IsVideo = false });
                    }
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
            if (string.IsNullOrWhiteSpace(Content) && AttachedMedia.Count == 0)
                return;

            if (_db.CurrentUser == null) return;

            var mediaPaths = string.Join(",", AttachedMedia.Select(m => m.FilePath));

            var request = new 
            {
                UserId = _db.CurrentUser.Id,
                PetId = SelectedPets.FirstOrDefault() is Pet firstPet ? (int?)firstPet.Id : null,
                AuthorName = _db.CurrentUser.FullName,
                PetName = SelectedPets.Count > 0 ? string.Join(" and ", SelectedPets.Cast<Pet>().Select(p => p.Name)) : "",
                Content = Content,
                ImageUrls = mediaPaths
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
