using Orchard.ContentManagement.MetaData;
using Orchard.Data.Migration;
using Orchard.FileSystems.Media;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Migrations
{
    public class BaseMigrations : DataMigrationImpl
    {
        private readonly IStorageProvider _storageProvider;


        public BaseMigrations(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }
    
            
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

            SetupRepoPageContentType(ContentDefinitionManager, WellKnownConstants.DefaultRepoPageContentType);


            return 3;
        }


        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable(typeof(MarkdownPagePartRecord).Name,
                table => table
                    .DropColumn("Text")
                );

            ContentDefinitionManager.AlterTypeDefinition(WellKnownConstants.DefaultRepoPageContentType,
                cfg => cfg
                    .WithPart("CommonPart")
                    .WithPart("BodyPart", builder => builder
                        .WithSetting("BodyTypePartSettings.Flavor", "markdown"))
                );


            return 2;
        }

        public int UpdateFrom2()
        {
            if (!_storageProvider.FolderExists("ExternalPages")) return 3;

            if (!_storageProvider.FolderExists("_OrchardHUNModules"))
            {
                _storageProvider.CreateFolder("_OrchardHUNModules");
            }
            _storageProvider.RenameFolder("ExternalPages", "_OrchardHUNModules/ExternalPages");


            return 3;
        }


        public static void SetupRepoPageContentType(IContentDefinitionManager contentDefinitionManager, string pageContentTypeName)
        {
            contentDefinitionManager.AlterTypeDefinition(pageContentTypeName,
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
        }
    }
}