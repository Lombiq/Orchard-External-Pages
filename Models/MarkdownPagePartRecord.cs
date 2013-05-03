using Orchard.ContentManagement.Records;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePartRecord : ContentPartRecord
    {
        public virtual string RepoPath { get; set; }
    }
}