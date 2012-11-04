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
                    .Column<string>("Text", column => column.Unlimited())
                    .Column<string>("RepoPath", column => column.Unique())
                )
                .AlterTable(typeof(MarkdownPagePartRecord).Name,
                table => table
                    .CreateIndex("RepoPath", new string[] { "RepoPath" })
                );

            ContentDefinitionManager.AlterTypeDefinition(WellKnownConstants.RepoPageContentType,
                cfg => cfg
                    .WithPart("TitlePart")
                    .WithPart("AutoroutePart", builder => builder
                        .WithSetting("AutorouteSettings.AllowCustomPattern", "false")
                        .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "true")
                        .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Title', Pattern: '{Content.Slug}', Description: 'my-page'}]")
                        .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                    .WithPart(typeof(MarkdownPagePart).Name)
            );


            return 1;
        }
    }
}