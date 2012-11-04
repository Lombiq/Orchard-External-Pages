using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Migrations
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

            ContentDefinitionManager.AlterTypeDefinition("MarkdownRepoPage",
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