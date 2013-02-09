using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Handlers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPartHandler : ContentHandler
    {
        public Localizer T { get; set; }


        public BitbucketSettingsPartHandler(
            IRepository<BitbucketSettingsPartRecord> repository,
            Work<IRepository<BitbucketRepositoryDataRecord>> bbRepositoryWork)
        {
            Filters.Add(StorageFilter.For(repository));
            Filters.Add(new ActivatingFilter<BitbucketSettingsPart>("Site"));

            OnActivated<BitbucketSettingsPart>((context, part) =>
            {
                part.RepositoriesField.Loader(() => bbRepositoryWork.Value.Table.ToList());
            });

            T = NullLocalizer.Instance;
        }


        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;

            base.GetItemMetadata(context);

            var groupInfo = new GroupInfo(T("External Pages")); // Addig a new group to the "Settings" menu.
            groupInfo.Id = "ExternalPages";
            context.Metadata.EditorGroupInfo.Add(groupInfo);
        }
    }
}