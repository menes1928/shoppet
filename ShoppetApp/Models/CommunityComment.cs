namespace ShoppetApp.Models
{
    public class CommunityComment
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public int? ParentCommentId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Populated from the users table when loading (not stored on row)
        [SQLite.Ignore]
        public string AuthorName { get; set; } = string.Empty;

        [SQLite.Ignore]
        public string AuthorRole { get; set; } = "PetOwner";

        [SQLite.Ignore]
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - CreatedAt;
                if (span.TotalSeconds < 60) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return CreatedAt.ToString("MMM dd, yyyy");
            }
        }
    }
}