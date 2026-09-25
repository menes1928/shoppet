using CommunityToolkit.Mvvm.ComponentModel;

namespace ShoppetApp.Models
{
    public partial class CommunityComment : ObservableObject
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public int? ParentCommentId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string? ParentAuthorName { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        [ObservableProperty]
        private int _likeCount;

        [ObservableProperty]
        private bool _isLikedByMe;

        // Computed helpers
        public bool IsReply => ParentCommentId.HasValue;
        public bool HasParentQuote => IsReply && !string.IsNullOrEmpty(ParentAuthorName);

        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - CreatedAt;
                if (span.TotalSeconds < 60) return "1m";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d";
                return CreatedAt.ToString("MMM d");
            }
        }

        public string Initials => string.IsNullOrWhiteSpace(AuthorName) ? "U" : AuthorName.Substring(0, 1).ToUpper();
    }
}
