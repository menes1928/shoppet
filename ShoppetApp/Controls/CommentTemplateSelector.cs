using ShoppetApp.Models;

namespace ShoppetApp.Controls
{
    public class CommentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TopLevelTemplate { get; set; }
        public DataTemplate? ReplyTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            if (item is CommunityComment comment && comment.IsReply)
                return ReplyTemplate;
            return TopLevelTemplate;
        }
    }
}
