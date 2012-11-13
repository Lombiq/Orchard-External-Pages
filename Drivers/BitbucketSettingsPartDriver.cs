using System;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Drivers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPartDriver : ContentPartDriver<BitbucketSettingsPart>
    {
        private readonly IRepository<BitbucketRepositoryDataRecord> _repository;
        private readonly INotifier _notifier;

        protected override string Prefix
        {
            get { return "OrchardHUN.ExternalPages.BitbucketSettingsPart"; }
        }

        public Localizer T { get; set; }


        public BitbucketSettingsPartDriver(
            IRepository<BitbucketRepositoryDataRecord> repository,
            INotifier notifier)
        {
            _repository = repository;
            _notifier = notifier;

            T = NullLocalizer.Instance;
        }


        // GET
        protected override DriverResult Editor(BitbucketSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_BitbucketSettings_SiteSettings",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts.BitbucketSettings.SiteSettings",
                    Model: part,
                    Prefix: Prefix)).OnGroup("ExternalPagesSettings");
        }

        // POST
        protected override DriverResult Editor(BitbucketSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            updater.TryUpdateModel(part, Prefix, null, null);

            if (part.Repositories != null)
            {
                foreach (var repository in part.Repositories)
                {
                    var original = _repository.Get(repository.Id);
                    if (original != null)
                    {
                        original.AccountName = repository.AccountName;
                        original.Slug = repository.Slug;
                        original.Username = repository.Username;
                        if (!String.IsNullOrEmpty(repository.Password) || String.IsNullOrEmpty(repository.Username)) original.Password = repository.Password;
                        original.MirrorFiles = repository.MirrorFiles;
                        original.MaximalFileSizeKB = repository.MaximalFileSizeKB;
                        original.UrlMappingsDefinition = repository.UrlMappingsDefinition;
                    }
                }
            }

            if (part.NewRepository != null && !String.IsNullOrEmpty(part.NewRepository.AccountName))
            {
                _repository.Create(part.NewRepository);
                _notifier.Information(T("The new repository entry was created. Before it wil be updated you should populate it first."));
            }

            _repository.Flush();

            return Editor(part, shapeHelper);
        }
    }
}