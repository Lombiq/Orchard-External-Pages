using Orchard.ContentManagement;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePart : ContentPart<MarkdownPagePartRecord>
    {
        public string RepoPath
        {
            get { return Record.RepoPath; }
            set { Record.RepoPath = value; }
        }
    }
}