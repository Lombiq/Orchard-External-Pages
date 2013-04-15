using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePartRecord : ContentPartRecord
    {
        public virtual string RepoPath { get; set; }
    }
}