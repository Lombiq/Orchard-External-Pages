using MarkdownSharp;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Handlers
{
    public class MarkdownPagePartHandler : ContentHandler
    {
        public MarkdownPagePartHandler(IRepository<MarkdownPagePartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));
        }
    }
}