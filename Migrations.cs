using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket
{
    public class Migrations : DataMigrationImpl
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

            SchemaBuilder.CreateTable(typeof(RepositorySettingsRecord).Name,
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("AccountName")
                    .Column<string>("Slug")
                    .Column<string>("Username")
                    .Column<string>("Password")
                    .Column<bool>("MirrorFiles")
                    .Column<int>("MaximalFileSizeKB")
                    .Column<string>("UrlMappingsDefinition")
                    .Column<string>("LastNode")
                );


            return 1;
        }
    }
}