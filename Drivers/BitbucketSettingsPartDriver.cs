using System;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Models;
using OrchardHUN.ExternalPages.Services.Bitbucket;

namespace OrchardHUN.ExternalPages.Drivers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPartDriver : ContentPartDriver<BitbucketSettingsPart>
    {
        private readonly IBitbucketService _bitbucketService;
        private readonly INotifier _notifier;

        protected override string Prefix
        {
            get { return "OrchardHUN.ExternalPages.BitbucketSettingsPart"; }
        }

        public Localizer T { get; set; }


        public BitbucketSettingsPartDriver(
            IBitbucketService bitbucketService,
            INotifier notifier)
        {
            _bitbucketService = bitbucketService;
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
                    Prefix: Prefix)).OnGroup("ExternalPages");
        }

        // POST
        protected override DriverResult Editor(BitbucketSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            updater.TryUpdateModel(part, Prefix, null, null);

            if (part.Repositories != null)
            {
                foreach (var repository in part.Repositories)
                {
                    var savedRepository = _bitbucketService.RepositoryDataRepository.Get(repository.Id);
                    if (savedRepository != null)
                    {
                        savedRepository.AccountName = repository.AccountName;
                        savedRepository.Slug = repository.Slug;
                        savedRepository.Username = repository.Username;
                        if (!String.IsNullOrEmpty(repository.Password) || String.IsNullOrEmpty(repository.Username)) savedRepository.Password = repository.Password;
                        savedRepository.MirrorFiles = repository.MirrorFiles;
                        savedRepository.MaximalFileSizeKB = repository.MaximalFileSizeKB;

                        if (savedRepository.UrlMappingsDefinition != repository.UrlMappingsDefinition)
                        {
                            savedRepository.UrlMappingsDefinition = repository.UrlMappingsDefinition;


                        }
                    }
                }
            }

            if (part.NewRepository != null && !String.IsNullOrEmpty(part.NewRepository.AccountName))
            {
                _bitbucketService.RepositoryDataRepository.Create(part.NewRepository);
                _notifier.Information(T("The new repository entry was created. Before it wil be updated you should populate it first."));
            }

            _bitbucketService.RepositoryDataRepository.Flush();

            return Editor(part, shapeHelper);
        }
    }
}