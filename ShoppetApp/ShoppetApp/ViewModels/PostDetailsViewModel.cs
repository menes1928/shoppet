using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppetApp.Models;
using ShoppetApp.Services;

namespace ShoppetApp.ViewModels
{
    [QueryProperty(nameof(Post), "Post")]
    public partial class PostDetailsViewModel : ObservableObject
    {
        private readonly ApiService _api;
        private readonly DatabaseService _db;

        [ObservableProperty]
        private CommunityPost? _post;

        [ObservableProperty]
        private ObservableCollection<CommunityComment> _comments = new();

        [ObservableProperty]
        private string _newCommentText = string.Empty;

        [ObservableProperty]
        private bool _isRefreshing;

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        public PostDetailsViewModel(ApiService api, DatabaseService db)
        {
            _api = api;
            _db = db;
        }

        async partial void OnPostChanged(CommunityPost? value)
        {
            if (value != null)
            {
                await LoadCommentsAsync();
            }
        }

        [RelayCommand]
        public async Task LoadCommentsAsync()
        {
            if (Post == null) return;
            
            IsRefreshing = true;
            try
            {
                var data = await _api.GetCommentsAsync(Post.Id);
                Comments.Clear();
                foreach (var c in data)
                {
                    Comments.Add(c);
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task ToggleLikeAsync()
        {
            if (Post == null || _db.CurrentUser == null) return;

            Post.IsLikedByMe = !Post.IsLikedByMe;
            Post.LikesCount += Post.IsLikedByMe ? 1 : -1;

            int userId = _db.CurrentUser.Id;
            var newStatus = await _api.ToggleLikeAsync(Post.Id, userId);
            
            if (newStatus != Post.IsLikedByMe)
            {
                Post.IsLikedByMe = newStatus;
                Post.LikesCount += Post.IsLikedByMe ? 1 : -1;
            }
        }

        [RelayCommand]
        private async Task SendCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCommentText) || Post == null || _db.CurrentUser == null) return;

            var success = await _api.AddCommentAsync(Post.Id, _db.CurrentUser.Id, NewCommentText);
            if (success)
            {
                NewCommentText = string.Empty;
                Post.CommentsCount++;
                await LoadCommentsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to add comment. Try again.", "OK");
            }
        }
    }
}


