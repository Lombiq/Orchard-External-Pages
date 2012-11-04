using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace OrchardHUN.ExternalPages.Models
{
    public class MarkdownPagePartRecord : ContentPartRecord
    {
        [StringLengthMax]
        public virtual string Text { get; set; }
        public virtual string RepoPath { get; set; }
    }
}