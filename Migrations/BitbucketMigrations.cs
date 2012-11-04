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
    public class BitbucketMigrations : DataMigrationImpl
    {
        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(BitbucketRepositorySettingsRecord).Name,
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