using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;

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