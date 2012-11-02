using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MarkdownSharp;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Handlers
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