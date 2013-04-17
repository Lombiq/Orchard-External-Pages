using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.Data.Migration;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Migrations
{
    public class BaseMigrations : DataMigrationImpl
    {
        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(MarkdownPagePartRecord).Name,
                table => table
                    .ContentPartRecord()
                    .Column<string>("RepoPath", column => column.Unique())
                )
                .AlterTable(typeof(MarkdownPagePartRecord).Name,
                table => table
                    .CreateIndex("RepoPath", new string[] { "RepoPath" })
                );

            ContentDefinitionManager.AlterTypeDefinition(WellKnownConstants.RepoPageContentType,
                cfg => cfg
                    .WithPart("TitlePart")
                    .WithPart("CommonPart")
                    .WithPart("AutoroutePart", builder => builder
                        .WithSetting("AutorouteSettings.AllowCustomPattern", "false")
                        .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "true")
                        .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'my-page'}]")
                        .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                    .WithPart(typeof(MarkdownPagePart).Name)
                    .WithPart("BodyPart", builder => builder
                        .WithSetting("BodyTypePartSettings.Flavor", "markdown"))
            );


            return 2;
        }


        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable(typeof(MarkdownPagePartRecord).Name,
                table => table
                .DropColumn("Text")
            );

            ContentDefinitionManager.AlterTypeDefinition(WellKnownConstants.RepoPageContentType,
                cfg => cfg
                    .WithPart("CommonPart")
                    .WithPart("BodyPart", builder => builder
                        .WithSetting("BodyTypePartSettings.Flavor", "markdown"))
            );


            return 2;
        }
    }
}