using System;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Migrations;
using OrchardHUN.ExternalPages.Models;
using OrchardHUN.ExternalPages.Services.Bitbucket;

namespace OrchardHUN.ExternalPages.Drivers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPartDriver : ContentPartDriver<BitbucketSettingsPart>
    {
        private readonly IBitbucketService _bitbucketService;
        private readonly INotifier _notifier;
        private readonly IEncryptionService _encryptionService;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        protected override string Prefix
        {
            get { return "OrchardHUN.ExternalPages.BitbucketSettingsPart"; }
        }

        public Localizer T { get; set; }


        public BitbucketSettingsPartDriver(
            IBitbucketService bitbucketService,
            INotifier notifier,
            IEncryptionService encryptionService,
            IContentDefinitionManager contentDefinitionManager)
        {
            _bitbucketService = bitbucketService;
            _notifier = notifier;
            _encryptionService = encryptionService;
            _contentDefinitionManager = contentDefinitionManager;

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

            if (part.IsBeingSaved)
            {
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
                            if (!string.IsNullOrEmpty(repository.Password) || string.IsNullOrEmpty(repository.Username)) savedRepository.Password = repository.Password;
                            savedRepository.PageContentTypeName = repository.PageContentTypeName;
                            CreateOrUpdatePageType(repository.PageContentTypeName);
                            savedRepository.MirrorFiles = repository.MirrorFiles;
                            savedRepository.MaximalFileSizeKB = repository.MaximalFileSizeKB;
                            savedRepository.SetPasswordEncrypted(_encryptionService, repository.Password);

                            if (savedRepository.UrlMappingsDefinition != repository.UrlMappingsDefinition)
                            {
                                savedRepository.UrlMappingsDefinition = repository.UrlMappingsDefinition;
                            }
                        }
                    }
                }

                if (part.NewRepository != null && !string.IsNullOrEmpty(part.NewRepository.AccountName))
                {
                    part.NewRepository.SetPasswordEncrypted(_encryptionService, part.NewRepository.Password);
                    CreateOrUpdatePageType(part.NewRepository.PageContentTypeName);
                    _bitbucketService.RepositoryDataRepository.Create(part.NewRepository);
                    _notifier.Information(T("The new repository entry was created. Before it wil be updated you should populate it first."));
                }

                _bitbucketService.RepositoryDataRepository.Flush(); 
            }

            return Editor(part, shapeHelper);
        }


        private void CreateOrUpdatePageType(string pageContentTypeName)
        {
            BaseMigrations.SetupRepoPageContentType(_contentDefinitionManager, pageContentTypeName);
        }
    }
}