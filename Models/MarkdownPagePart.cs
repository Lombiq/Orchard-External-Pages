using Orchard.ContentManagement;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePart : ContentPart<MarkdownPagePartRecord>
    {
        public string RepoPath
        {
            get { return Retrieve(x => x.RepoPath); }
            set { Store(x => x.RepoPath, value); }
        }
    }
}