using ShoppetApp.Models;
using ShoppetApp.Services;
using System.Diagnostics;

namespace ShoppetApp.Pages;

public partial class CommunityPage : ContentPage
{
    private readonly DatabaseService _db;

    private readonly List<string> _attachedPhotoPaths = new();

    private CommunityPost? _editingPost;

    private List<User> _allUsers = new();

    public CommunityPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;

        UpdateAttachmentLabel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // -----------------------------
            // CURRENT USER
            // -----------------------------
            if (_db.CurrentUser != null)
            {
                UserGreetingLabel.Text =
                    $"Posting as: {_db.CurrentUser.FullName} ({_db.CurrentUser.Role})";
            }
            else
            {
                UserGreetingLabel.Text =
                    "Browsing Community Feed (Guest Mode)";
            }

            // -----------------------------
            // LOAD USERS FOR @MENTIONS
            // -----------------------------
            try
            {
                _allUsers = await _db.GetUsersAsync()
                             ?? new List<User>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Could not load users: {ex.Message}");

                _allUsers = new List<User>();
            }

            // -----------------------------
            // LOAD COMMUNITY POSTS
            // -----------------------------
            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"OnAppearing Error: {ex}");
        }
    }

    // =========================================================
    // LOAD POSTS
    // =========================================================

    private async Task LoadPostsFromDatabaseAsync()
    {
        try
        {
            var postsFromDb =
                await _db.GetCommunityPostsAsync();

            PostsCollection.ItemsSource = postsFromDb;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Load Posts Error: {ex}");

            await DisplayAlert(
                "Database Error",
                $"Could not load community posts.\n\n{ex.Message}",
                "OK");
        }
    }

    // =========================================================
    // @MENTION SEARCH
    // =========================================================

    private void OnPostContentTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        try
        {
            string newText =
                e.NewTextValue ?? string.Empty;

            if (string.IsNullOrWhiteSpace(newText))
            {
                HideMentionSuggestions();
                return;
            }

            int lastAtIndex =
                newText.LastIndexOf('@');

            if (lastAtIndex < 0)
            {
                HideMentionSuggestions();
                return;
            }

            string query =
                newText.Substring(lastAtIndex + 1);

            // Stop suggestions once a space is typed
            if (query.Contains(' '))
            {
                HideMentionSuggestions();
                return;
            }

            if (_allUsers == null ||
                !_allUsers.Any())
            {
                HideMentionSuggestions();
                return;
            }

            var matchingUsers =
                _allUsers
                    .Where(u =>
                        !string.IsNullOrWhiteSpace(u.FullName) &&
                        u.FullName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToList();

            if (matchingUsers.Any())
            {
                MentionSuggestionsView.ItemsSource =
                    matchingUsers;

                MentionSuggestionsView.IsVisible = true;
            }
            else
            {
                HideMentionSuggestions();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Mention Error: {ex.Message}");

            HideMentionSuggestions();
        }
    }

    // =========================================================
    // SELECT @MENTION
    // =========================================================

    private void OnMentionSelected(
        object sender,
        SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault()
                is not User selectedUser)
            {
                return;
            }

            string currentText =
                PostContentEditor.Text ?? string.Empty;

            int lastAtIndex =
                currentText.LastIndexOf('@');

            if (lastAtIndex >= 0)
            {
                string textBeforeAt =
                    currentText.Substring(
                        0,
                        lastAtIndex);

                PostContentEditor.Text =
                    $"{textBeforeAt}@{selectedUser.FullName} ";
            }

            HideMentionSuggestions();

            PostContentEditor.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Mention Selection Error: {ex.Message}");
        }
    }

    private void HideMentionSuggestions()
    {
        if (MentionSuggestionsView == null)
            return;

        MentionSuggestionsView.IsVisible = false;
        MentionSuggestionsView.ItemsSource = null;
    }

    // =========================================================
    // ATTACH PHOTO
    // =========================================================

    private async void OnAttachPhotosClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            var result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Select a photo for your post",

                        FileTypes =
                            FilePickerFileType.Images
                    });

            // User closed/cancelled the picker
            if (result == null)
                return;

            string filePath =
                result.FullPath;

            // Some platforms may not provide
            // a usable physical FullPath.
            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                filePath =
                    Path.Combine(
                        FileSystem.CacheDirectory,
                        result.FileName);

                await using var inputStream =
                    await result.OpenReadAsync();

                await using var outputStream =
                    File.Create(filePath);

                await inputStream.CopyToAsync(
                    outputStream);
            }

            // This version supports one attached
            // photo at a time.
            _attachedPhotoPaths.Clear();

            _attachedPhotoPaths.Add(filePath);

            UpdateAttachmentLabel();
        }
        catch (OperationCanceledException)
        {
            // User cancelled the picker.
            // Do nothing.
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"File Picker Error: {ex}");

            await DisplayAlert(
                "File Picker Error",
                $"Could not select the photo.\n\n{ex.Message}",
                "OK");
        }
    }

    // =========================================================
    // ATTACHMENT LABEL
    // =========================================================

    private void UpdateAttachmentLabel()
    {
        if (AttachedFilesCountLabel == null)
            return;

        if (_attachedPhotoPaths.Any())
        {
            AttachedFilesCountLabel.Text =
                $"{_attachedPhotoPaths.Count} photo attached";

            AttachedFilesCountLabel.TextColor =
                Color.FromArgb("#5C766D");
        }
        else
        {
            AttachedFilesCountLabel.Text =
                "No photos attached";

            AttachedFilesCountLabel.TextColor =
                Color.FromArgb("#7C8782");
        }
    }

    // =========================================================
    // CREATE / UPDATE POST
    // =========================================================

    private async void OnPostClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            // -----------------------------
            // VALIDATE PET NAME
            // -----------------------------
            if (string.IsNullOrWhiteSpace(
                PetNameEntry?.Text))
            {
                await DisplayAlert(
                    "Validation Required",
                    "Please enter your pet's name.",
                    "OK");

                return;
            }

            // -----------------------------
            // VALIDATE POST CONTENT
            // -----------------------------
            if (string.IsNullOrWhiteSpace(
                PostContentEditor?.Text))
            {
                await DisplayAlert(
                    "Validation Required",
                    "Please write something about your pet.",
                    "OK");

                return;
            }

            string petName =
                PetNameEntry.Text.Trim();

            string content =
                PostContentEditor.Text.Trim();

            string authorName =
                _db.CurrentUser?.FullName
                ?? "Community Pet Parent";

            string? combinedImagePaths =
                _attachedPhotoPaths.Any()
                    ? string.Join(
                        ",",
                        _attachedPhotoPaths)
                    : null;

            // =================================================
            // EDIT EXISTING POST
            // =================================================

            if (_editingPost != null)
            {
                _editingPost.PetName =
                    petName;

                _editingPost.Content =
                    content;

                if (!string.IsNullOrWhiteSpace(
                    combinedImagePaths))
                {
                    _editingPost.ImageUrls =
                        combinedImagePaths;
                }

                _editingPost.IsEdited = true;

                await _db.UpdateCommunityPostAsync(
                    _editingPost);

                _editingPost = null;
            }

            // =================================================
            // CREATE NEW POST
            // =================================================

            else
            {
                var newPost =
                    new CommunityPost
                    {
                        AuthorName =
                            authorName,

                        PetName =
                            petName,

                        Content =
                            content,

                        ImageUrls =
                            combinedImagePaths,

                        Timestamp =
                            DateTime.Now,

                        IsEdited =
                            false
                    };

                await _db.SaveCommunityPostAsync(
                    newPost);
            }

            // -----------------------------
            // RESET FORM
            // -----------------------------
            ClearPostEditor();

            // -----------------------------
            // REFRESH FEED
            // -----------------------------
            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Post Error: {ex}");

            await DisplayAlert(
                "Error",
                $"Could not publish the post.\n\n{ex.Message}",
                "OK");
        }
    }

    // =========================================================
    // EDIT POST
    // =========================================================

    private void OnEditPostClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (sender is not Button btn)
                return;

            if (btn.CommandParameter
                is not CommunityPost post)
            {
                return;
            }

            _editingPost = post;

            PetNameEntry.Text =
                post.PetName ?? string.Empty;

            PostContentEditor.Text =
                post.Content ?? string.Empty;

            _attachedPhotoPaths.Clear();

            if (!string.IsNullOrWhiteSpace(
                post.ImageUrls))
            {
                var existingImages =
                    post.ImageUrls.Split(
                        ',',
                        StringSplitOptions
                            .RemoveEmptyEntries);

                _attachedPhotoPaths.AddRange(
                    existingImages);
            }

            UpdateAttachmentLabel();

            PostContentEditor.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Edit Post Error: {ex}");
        }
    }

    // =========================================================
    // DELETE POST
    // =========================================================

    private async void OnDeletePostClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (sender is not Button btn)
                return;

            if (btn.CommandParameter
                is not CommunityPost post)
            {
                return;
            }

            bool confirm =
                await DisplayAlert(
                    "Delete Post",
                    "Are you sure you want to remove this community post?",
                    "Yes",
                    "No");

            if (!confirm)
                return;

            await _db.DeleteCommunityPostAsync(
                post);

            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Delete Post Error: {ex}");

            await DisplayAlert(
                "Error",
                $"Could not delete the post.\n\n{ex.Message}",
                "OK");
        }
    }

    // =========================================================
    // CLEAR POST FORM
    // =========================================================

    private void ClearPostEditor()
    {
        PetNameEntry.Text =
            string.Empty;

        PostContentEditor.Text =
            string.Empty;

        _attachedPhotoPaths.Clear();

        _editingPost = null;

        HideMentionSuggestions();

        UpdateAttachmentLabel();
    }
}