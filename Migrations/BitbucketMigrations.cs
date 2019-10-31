using Orchard.Data;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Orchard.Security;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Migrations
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketMigrations : DataMigrationImpl
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IRepository<BitbucketRepositoryDataRecord> _repository;


        public BitbucketMigrations(IEncryptionService encryptionService, IRepository<BitbucketRepositoryDataRecord> repository)
        {
            _encryptionService = encryptionService;
            _repository = repository;
        }


        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("AccountName")
                    .Column<string>("Slug")
                    .Column<string>("Username")
                    .Column<string>("Password", column => column.Unlimited())
                    .Column<string>("PageContentTypeName", column => column.WithLength(1024))
                    .Column<bool>("MirrorFiles")
                    .Column<int>("MaximalFileSizeKB")
                    .Column<string>("UrlMappingsDefinition", column => column.Unlimited())
                    .Column<string>("LastCheckedNode")
                    .Column<string>("LastProcessedNode")
                );


            return 8;
        }

        public int UpdateFrom1()
        {
            // Only BitbucketSettingsPartRecord table alteration was here.

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

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .AlterColumn("Password", column => column.WithType(System.Data.DbType.String).WithLength(2000))
                );

            foreach (var repoData in _repository.Table)
            {
                repoData.SetPasswordEncrypted(_encryptionService, repoData.Password);
            }


            return 4;
        }

        public int UpdateFrom4()
        {
            SchemaBuilder.AlterTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .AlterColumn("Password", column => column.WithType(System.Data.DbType.String).WithLength(4000))
                );

            return 5;
        }

        public int UpdateFrom5()
        {
            SchemaBuilder.DropTable("BitbucketSettingsPartRecord");

            return 6;
        }

        public int UpdateFrom6()
        {
            SchemaBuilder.AlterTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .AlterColumn("Password", column => column.WithType(System.Data.DbType.String).Unlimited())
                );

            return 7;
        }

        public int UpdateFrom7()
        {
            SchemaBuilder.AlterTable(typeof(BitbucketRepositoryDataRecord).Name,
                table => table
                    .AddColumn<string>("PageContentTypeName", column => column.WithLength(1024))
                );

            return 8;
        }
    }
}