using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Localization;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Handlers
{
    public class BitbucketSettingsPartHandler : ContentHandler
    {
        public Localizer T { get; set; }


        public BitbucketSettingsPartHandler(Work<IRepository<BitbucketRepositorySettingsRecord>> repositoryWork)
        {
            Filters.Add(new ActivatingFilter<BitbucketSettingsPart>("Site"));

            OnActivated<BitbucketSettingsPart>((context, part) =>
            {
                part.RepositoriesField.Loader(() => repositoryWork.Value.Table.ToList());
            });

            T = NullLocalizer.Instance;
        }


        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;

            base.GetItemMetadata(context);

            var groupInfo = new GroupInfo(T("External Pages Settings")); // Addig a new group to the "Settings" menu.
            groupInfo.Id = "ExternalPagesSettings";
            context.Metadata.EditorGroupInfo.Add(groupInfo);
        }
    }
}