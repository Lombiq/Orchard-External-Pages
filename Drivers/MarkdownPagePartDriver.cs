using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Drivers
{
    public class MarkdownPagePartDriver : ContentPartDriver<MarkdownPagePart>
    {
        protected override string Prefix
        {
            get { return "OrchardHUN.Bitbucket.MarkdownPagePart"; }
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

        protected override DriverResult Editor(MarkdownPagePart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_Script_Edit",
                    () => shapeHelper.EditorTemplate(
                            TemplateName: "Parts.Script",
                            Model: part,
                            Prefix: Prefix));
        }

        protected override DriverResult Editor(MarkdownPagePart part, IUpdateModel updater, dynamic shapeHelper)
        {
            updater.TryUpdateModel(part, Prefix, null, null);
            return Editor(part, shapeHelper);
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