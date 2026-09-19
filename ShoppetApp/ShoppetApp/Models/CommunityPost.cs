namespace ShoppetApp.Models
{
    public class CommunityPost
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrls { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsEdited { get; set; } = false;

        [SQLite.Ignore]
        public List<string> ImageList => string.IsNullOrEmpty(ImageUrls)
            ? new List<string>()
            : ImageUrls.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        [SQLite.Ignore]
        public bool HasImages => ImageList.Any();

        // Live Relative Time Formatting
        [SQLite.Ignore]
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - Timestamp;
                if (span.TotalSeconds < 60) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return Timestamp.ToString("MMM dd, yyyy");
            }
        }

        [SQLite.Ignore]
        public string FormattedTime => $"{TimeAgo}" + (IsEdited ? " (Edited)" : "");
    }
}