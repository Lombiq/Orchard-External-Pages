using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace OrchardHUN.Bitbucket.Models
{
    public class MarkdownPagePartRecord : ContentPartRecord
    {
        [StringLengthMax]
        public virtual string Text { get; set; }
        public virtual string RepoPath { get; set; }
    }
}