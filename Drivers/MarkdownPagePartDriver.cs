using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Drivers
{
    public class MarkdownPagePartDriver : ContentPartDriver<MarkdownPagePart>
    {
        protected override string Prefix
        {
            get { return "OrchardHUN.ExternalPages.MarkdownPagePart"; }
        }


        protected override DriverResult Display(MarkdownPagePart part, string displayType, dynamic shapeHelper)
        {
            return Combined(
                ContentShape("Parts_MarkdownPage_Text",
                    () => shapeHelper.Parts_MarkdownPage_Text()),
                ContentShape("Parts_MarkdownPage_Text_Summary",
                    () => shapeHelper.Parts_MarkdownPage_Text_Summary())
                );
        }

        protected override void Exporting(MarkdownPagePart part, ExportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.Element(partName).SetAttributeValue("Text", part.Text);
        }

        protected override void Importing(MarkdownPagePart part, ImportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.ImportAttribute(partName, "Text", value => part.Text = value);
        }
    }
}