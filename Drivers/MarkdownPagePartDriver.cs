using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Drivers
{
    public class MarkdownPagePartDriver : ContentPartDriver<MarkdownPagePart>
    {
        protected override void Exporting(MarkdownPagePart part, ExportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.Element(partName).SetAttributeValue("RepoPath", part.RepoPath);
        }

        protected override void Importing(MarkdownPagePart part, ImportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.ImportAttribute(partName, "RepoPath", value => part.RepoPath = value);
        }
    }
}