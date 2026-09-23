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

    // ⭐ Read from _db.CurrentUser first, fall back to Preferences.
    //    This is the single source of truth for "who is logged in".
    private int CurrentUserId =>
        _db.CurrentUser != null && _db.CurrentUser.Id > 0
            ? _db.CurrentUser.Id
            : Preferences.Get("LoggedInUserId", 0);

    private string CurrentUserName =>
        !string.IsNullOrEmpty(_db.CurrentUser?.FullName)
            ? _db.CurrentUser!.FullName
            : Preferences.Get("LoggedInUserName", "Community Pet Parent");

    private string CurrentUserRole =>
        !string.IsNullOrEmpty(_db.CurrentUser?.Role)
            ? _db.CurrentUser!.Role
            : Preferences.Get("LoggedInUserRole", "PetOwner");

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (_db.CurrentUser != null)
                UserGreetingLabel.Text = $"Posting as: {_db.CurrentUser.FullName} ({_db.CurrentUser.Role})";
            else if (CurrentUserId > 0)
                UserGreetingLabel.Text = $"Posting as: {CurrentUserName} ({CurrentUserRole})";
            else
                UserGreetingLabel.Text = "Browsing Community Feed (Guest Mode)";

            try { _allUsers = await _db.GetUsersAsync() ?? new List<User>(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load users: {ex.Message}");
                _allUsers = new();
            }

            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex) { Debug.WriteLine($"OnAppearing Error: {ex}"); }
    }

    // =========================================================
    // LOAD POSTS
    // =========================================================
    private async Task LoadPostsFromDatabaseAsync()
    {
        try
        {
            var postsFromDb = await _db.GetCommunityPostsAsync(CurrentUserId);

            foreach (var p in postsFromDb)
                p.CanEdit = CanUserEdit(p);

            PostsCollection.ItemsSource = postsFromDb;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Load Posts Error: {ex}");
            await DisplayAlert("Database Error", $"Could not load community posts.\n\n{ex.Message}", "OK");
        }
    }

    private bool CanUserEdit(CommunityPost post)
    {
        if (CurrentUserId <= 0) return false;
        if (CurrentUserRole == "Admin") return true;
        return post.UserId == CurrentUserId;
    }

    // ⭐⭐⭐ THE FIX for Windows CollectionView not repainting ⭐⭐⭐
    private void RefreshPostsCollection()
    {
        var current = PostsCollection.ItemsSource;
        PostsCollection.ItemsSource = null;
        PostsCollection.ItemsSource = current;
    }

    // =========================================================
    // LIKE
    // =========================================================
    private async void OnLikeTapped(object sender, TappedEventArgs e)
    {
        Debug.WriteLine("=== [LIKE] tapped ===");

        // Try to resolve the post from e.Parameter first, then BindingContext
        CommunityPost? post = e.Parameter as CommunityPost;
        if (post == null && sender is TapGestureRecognizer tr)
            post = tr.BindingContext as CommunityPost;

        if (post == null)
        {
            Debug.WriteLine("[LIKE] Could not resolve post");
            return;
        }

        Debug.WriteLine($"[LIKE] postId={post.Id} CurrentUserId={CurrentUserId}");

        if (CurrentUserId <= 0)
        {
            await DisplayAlert("Sign in required", "Please log in to like posts.", "OK");
            return;
        }

        // Find the Border to animate (optional — works on some platforms)
        VisualElement? ve = null;
        if (sender is TapGestureRecognizer tap && tap.Parent is VisualElement parentVe)
            ve = parentVe;

        try
        {
            if (ve != null)
                await ve.ScaleTo(0.9, 60, Easing.CubicOut);

            var newCount = await _db.ToggleLikeAsync(post.Id, CurrentUserId);
            Debug.WriteLine($"[LIKE] ToggleLikeAsync returned {newCount}");

            post.LikeCount = newCount;
            post.IsLikedByMe = !post.IsLikedByMe;

            Debug.WriteLine($"[LIKE] post updated: LikeCount={post.LikeCount}, IsLikedByMe={post.IsLikedByMe}");

            // ⭐ Force the CollectionView to repaint
            RefreshPostsCollection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LIKE] Exception: {ex}");
            await DisplayAlert("Like error", ex.Message, "OK");
        }
        finally
        {
            if (ve != null)
            {
                await ve.ScaleTo(1.0, 90, Easing.CubicIn);
                ve.Scale = 1;
            }
        }
    }

    // =========================================================
    // COMMENTS — expand/collapse
    // =========================================================
    private async void OnToggleCommentsTapped(object sender, TappedEventArgs e)
    {
        Debug.WriteLine("=== [COMMENTS] toggle tapped ===");

        CommunityPost? post = e.Parameter as CommunityPost;
        if (post == null && sender is TapGestureRecognizer tr)
            post = tr.BindingContext as CommunityPost;

        if (post == null)
        {
            Debug.WriteLine("[COMMENTS] Could not resolve post");
            return;
        }

        try
        {
            if (post.IsCommentsExpanded)
            {
                post.IsCommentsExpanded = false;
                RefreshPostsCollection();
                return;
            }

            Debug.WriteLine($"[COMMENTS] Loading for postId={post.Id}");
            var comments = await _db.GetCommentsAsync(post.Id);
            Debug.WriteLine($"[COMMENTS] Loaded {comments.Count} comments");

            post.Comments.Clear();
            foreach (var c in comments)
                post.Comments.Add(c);

            post.CommentCount = post.Comments.Count;
            post.IsCommentsExpanded = true;

            RefreshPostsCollection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[COMMENTS] Exception: {ex}");
            await DisplayAlert("Comments error", ex.Message, "OK");
        }
    }

    // =========================================================
    // COMMENTS — send
    // =========================================================
    private async void OnSendCommentClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("=== [SEND COMMENT] clicked ===");

        try
        {
            if (sender is not Button btn)
            {
                Debug.WriteLine("[SEND] sender not Button");
                return;
            }

            // Resolve the post from CommandParameter or BindingContext
            CommunityPost? post = btn.CommandParameter as CommunityPost
                                ?? btn.BindingContext as CommunityPost;

            if (post == null)
            {
                Debug.WriteLine("[SEND] Could not resolve post");
                await DisplayAlert("Bug", "Could not find which post to comment on.", "OK");
                return;
            }

            Debug.WriteLine($"[SEND] postId={post.Id} CurrentUserId={CurrentUserId}");

            if (CurrentUserId <= 0)
            {
                await DisplayAlert("Sign in required", "Please log in to comment.", "OK");
                return;
            }

            // Find the Entry — walk up the tree and search recursively
            Entry? entry = FindEntryFor(btn);
            if (entry == null)
            {
                Debug.WriteLine("[SEND] Could not find comment Entry");
                await DisplayAlert("Bug", "Could not find comment textbox.", "OK");
                return;
            }

            string text = (entry.Text ?? string.Empty).Trim();
            Debug.WriteLine($"[SEND] text='{text}'");

            if (string.IsNullOrWhiteSpace(text))
            {
                await DisplayAlert("Empty comment", "Please write something first.", "OK");
                return;
            }

            var comment = new CommunityComment
            {
                PostId = post.Id,
                UserId = CurrentUserId,
                Content = text,
                CreatedAt = DateTime.Now,
                AuthorName = _db.CurrentUser?.FullName ?? CurrentUserName,
                AuthorRole = _db.CurrentUser?.Role ?? CurrentUserRole
            };

            Debug.WriteLine($"[SEND] Inserting postId={post.Id} userId={CurrentUserId}");
            int newId = await _db.AddCommentAsync(comment);
            Debug.WriteLine($"[SEND] AddCommentAsync returned {newId}");

            if (newId <= 0)
            {
                await DisplayAlert("Error",
                    "Could not save your comment. Check the Output window for details.", "OK");
                return;
            }

            comment.Id = newId;
            post.Comments.Add(comment);
            post.CommentCount = post.Comments.Count;
            entry.Text = string.Empty;

            Debug.WriteLine($"[SEND] SUCCESS - comment #{newId} added. Total now {post.CommentCount}");

            // ⭐ Force the CollectionView to repaint
            RefreshPostsCollection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SEND] Exception: {ex}");
            await DisplayAlert("Comment failed", ex.Message, "OK");
        }
    }

    // =========================================================
    // COMMENTS — Enter key submits
    // =========================================================
    private void OnCommentSubmitted(object sender, EventArgs e)
    {
        if (sender is not Entry entry) return;

        // Walk up until we find a Grid with a Button sibling
        Element? current = entry;
        for (int i = 0; i < 5 && current != null; i++)
        {
            current = current.Parent;
            if (current is Grid grid)
            {
                var sendButton = grid.Children.OfType<Button>().FirstOrDefault();
                if (sendButton != null)
                {
                    OnSendCommentClicked(sendButton, EventArgs.Empty);
                    return;
                }
            }
        }
    }

    // =========================================================
    // Recursive Entry finder — walks the tree from the button
    // =========================================================
    private static Entry? FindEntryFor(Button button)
    {
        Element? current = button;
        for (int i = 0; i < 6 && current != null; i++)
        {
            current = current.Parent;
            if (current == null) break;

            // Look inside the parent layout for an Entry
            var found = FindEntryRecursive(current);
            if (found != null) return found;
        }
        return null;
    }

    private static Entry? FindEntryRecursive(Element? element)
    {
        if (element == null) return null;
        if (element is Entry e) return e;

        if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                var found = FindEntryRecursive(child as Element);
                if (found != null) return found;
            }
        }
        else if (element is ContentView cv && cv.Content is Element content)
        {
            return FindEntryRecursive(content);
        }
        else if (element is Border border && border.Content is Element borderContent)
        {
            return FindEntryRecursive(borderContent);
        }

        return null;
    }

    // =========================================================
    // @MENTION SEARCH
    // =========================================================
    private void OnPostContentTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            string newText = e.NewTextValue ?? string.Empty;
            if (string.IsNullOrWhiteSpace(newText)) { HideMentionSuggestions(); return; }

            int lastAtIndex = newText.LastIndexOf('@');
            if (lastAtIndex < 0) { HideMentionSuggestions(); return; }

            string query = newText.Substring(lastAtIndex + 1);
            if (query.Contains(' ')) { HideMentionSuggestions(); return; }
            if (_allUsers == null || !_allUsers.Any()) { HideMentionSuggestions(); return; }

            var matchingUsers = _allUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.FullName) &&
                            u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();

            if (matchingUsers.Any())
            {
                MentionSuggestionsView.ItemsSource = matchingUsers;
                MentionSuggestionsView.IsVisible = true;
            }
            else HideMentionSuggestions();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Mention Error: {ex.Message}");
            HideMentionSuggestions();
        }
    }

    private void OnMentionSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is not User selectedUser) return;

            string currentText = PostContentEditor.Text ?? string.Empty;
            int lastAtIndex = currentText.LastIndexOf('@');

            if (lastAtIndex >= 0)
            {
                string textBeforeAt = currentText.Substring(0, lastAtIndex);
                PostContentEditor.Text = $"{textBeforeAt}@{selectedUser.FullName} ";
            }

            HideMentionSuggestions();
            PostContentEditor.Focus();
        }
        catch (Exception ex) { Debug.WriteLine($"Mention Selection Error: {ex.Message}"); }
    }

    private void HideMentionSuggestions()
    {
        if (MentionSuggestionsView == null) return;
        MentionSuggestionsView.IsVisible = false;
        MentionSuggestionsView.ItemsSource = null;
    }

    // =========================================================
    // ATTACH PHOTO
    // =========================================================
    private async void OnAttachPhotosClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a photo for your post",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null) return;

            string filePath = result.FullPath;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                filePath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
                await using var inputStream = await result.OpenReadAsync();
                await using var outputStream = File.Create(filePath);
                await inputStream.CopyToAsync(outputStream);
            }

            _attachedPhotoPaths.Clear();
            _attachedPhotoPaths.Add(filePath);
            UpdateAttachmentLabel();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"File Picker Error: {ex}");
            await DisplayAlert("File Picker Error", $"Could not select the photo.\n\n{ex.Message}", "OK");
        }
    }

    private void UpdateAttachmentLabel()
    {
        if (AttachedFilesCountLabel == null) return;

        if (_attachedPhotoPaths.Any())
        {
            AttachedFilesCountLabel.Text = $"{_attachedPhotoPaths.Count} photo attached";
            AttachedFilesCountLabel.TextColor = Color.FromArgb("#5C766D");
        }
        else
        {
            AttachedFilesCountLabel.Text = "No photos attached";
            AttachedFilesCountLabel.TextColor = Color.FromArgb("#7C8782");
        }
    }

    // =========================================================
    // CREATE / UPDATE POST
    // =========================================================
    private async void OnPostClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PetNameEntry?.Text))
            {
                await DisplayAlert("Validation Required", "Please enter your pet's name.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PostContentEditor?.Text))
            {
                await DisplayAlert("Validation Required", "Please write something about your pet.", "OK");
                return;
            }

            if (CurrentUserId <= 0)
            {
                await DisplayAlert("Sign in required", "Please log in to create posts.", "OK");
                return;
            }

            string petName = PetNameEntry.Text.Trim();
            string content = PostContentEditor.Text.Trim();
            string authorName = _db.CurrentUser?.FullName ?? CurrentUserName;
            string? combinedImagePaths = _attachedPhotoPaths.Any()
                ? string.Join(",", _attachedPhotoPaths) : null;

            if (_editingPost != null)
            {
                if (!CanUserEdit(_editingPost))
                {
                    await DisplayAlert("Not allowed", "You can only edit your own posts.", "OK");
                    _editingPost = null;
                    return;
                }

                _editingPost.PetName = petName;
                _editingPost.Content = content;
                if (!string.IsNullOrWhiteSpace(combinedImagePaths))
                    _editingPost.ImageUrls = combinedImagePaths;
                _editingPost.IsEdited = true;

                var updated = await _db.UpdateCommunityPostAsync(_editingPost);
                if (!updated)
                {
                    await DisplayAlert("Error", "Could not update the post.", "OK");
                    return;
                }
                _editingPost = null;
            }
            else
            {
                var newPost = new CommunityPost
                {
                    AuthorName = authorName,
                    PetName = petName,
                    Content = content,
                    ImageUrls = combinedImagePaths,
                    Timestamp = DateTime.Now,
                    IsEdited = false,
                    UserId = CurrentUserId
                };

                var newId = await _db.SaveCommunityPostAsync(newPost);
                if (newId <= 0)
                {
                    await DisplayAlert("Error", "Could not save the post to the database.", "OK");
                    return;
                }
            }

            ClearPostEditor();
            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Post Error: {ex}");
            await DisplayAlert("Error", $"Could not publish the post.\n\n{ex.Message}", "OK");
        }
    }

    // =========================================================
    // EDIT POST
    // =========================================================
    private void OnEditPostClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn) return;
            if (btn.CommandParameter is not CommunityPost post) return;

            if (!CanUserEdit(post))
            {
                DisplayAlert("Not allowed", "You can only edit your own posts.", "OK");
                return;
            }

            _editingPost = post;
            PetNameEntry.Text = post.PetName ?? string.Empty;
            PostContentEditor.Text = post.Content ?? string.Empty;

            _attachedPhotoPaths.Clear();
            if (!string.IsNullOrWhiteSpace(post.ImageUrls))
            {
                var existingImages = post.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
                _attachedPhotoPaths.AddRange(existingImages);
            }

            UpdateAttachmentLabel();
            PostContentEditor.Focus();
        }
        catch (Exception ex) { Debug.WriteLine($"Edit Post Error: {ex}"); }
    }

    // =========================================================
    // DELETE POST
    // =========================================================
    private async void OnDeletePostClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn) return;
            if (btn.CommandParameter is not CommunityPost post) return;

            if (!CanUserEdit(post))
            {
                await DisplayAlert("Not allowed", "You can only delete your own posts.", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Delete Post",
                "Are you sure you want to remove this community post?",
                "Yes", "No");

            if (!confirm) return;

            var deleted = await _db.DeleteCommunityPostAsync(post);
            if (!deleted)
            {
                await DisplayAlert("Error", "Could not delete the post.", "OK");
                return;
            }

            await LoadPostsFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Delete Post Error: {ex}");
            await DisplayAlert("Error", $"Could not delete the post.\n\n{ex.Message}", "OK");
        }
    }

    // =========================================================
    // CLEAR POST FORM
    // =========================================================
    private void ClearPostEditor()
    {
        PetNameEntry.Text = string.Empty;
        PostContentEditor.Text = string.Empty;
        _attachedPhotoPaths.Clear();
        _editingPost = null;
        HideMentionSuggestions();
        UpdateAttachmentLabel();
    }
}