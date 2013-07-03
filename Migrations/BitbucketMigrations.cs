using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Migrations
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketMigrations : DataMigrationImpl
    {
        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("AccountName")
                    .Column<string>("Slug")
                    .Column<string>("Username")
                    .Column<string>("Password")
                    .Column<bool>("MirrorFiles")
                    .Column<int>("MaximalFileSizeKB")
                    .Column<string>("UrlMappingsDefinition", column => column.Unlimited())
                    .Column<string>("LastCheckedNode")
                    .Column<int>("LastCheckedRevision")
                    .Column<string>("LastProcessedNode")
                    .Column<int>("LastProcessedRevision")
                );

            SchemaBuilder.CreateTable(typeof(BitbucketSettingsPartRecord).Name,
                table => table
                    .ContentPartRecord()
                    .Column<int>("MinutesBetweenPulls")
                );


            return 3;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.CreateTable(typeof(BitbucketSettingsPartRecord).Name,
                table => table
                    .ContentPartRecord()
                    .Column<int>("MinutesBetweenPulls")
                );


            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .AlterColumn("UrlMappingsDefinition", column => column.WithType(System.Data.DbType.String).Unlimited())
                );


            return 3;
        }
    }
}