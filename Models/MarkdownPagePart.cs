using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePart : ContentPart<MarkdownPagePartRecord> 
    {
        public string Text
        {
            get { return Record.Text; }
            set { Record.Text = value; }
        }

        private readonly LazyField<string> _html = new LazyField<string>();
        public LazyField<string> HtmlField { get { return _html; } }
        public string Html
        {
            get { return _html.Value; }
        }

        public string RepoPath {
            get { return Record.RepoPath; }
            set { Record.RepoPath = value; }
        }
    }
}