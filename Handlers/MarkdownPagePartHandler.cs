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

            OnIndexing<MarkdownPagePart>((context, part) =>
            {
                context.DocumentIndex.Add("markdownText", part.Text).RemoveTags().Analyze();
            });

            OnActivated<MarkdownPagePart>((context, part) =>
            {
                part.HtmlField.Loader(() => new Markdown().Transform(part.Text));
            });
        }
    }
}