using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Drivers
{
    public class BitbucketSettingsPartDriver : ContentPartDriver<BitbucketSettingsPart>
    {
        private readonly IRepository<BitbucketRepositorySettingsRecord> _repository;

        protected override string Prefix
        {
            get { return "OrchardHUN.Bitbucket.BitbucketSettingsPart"; }
        }


        public BitbucketSettingsPartDriver(IRepository<BitbucketRepositorySettingsRecord> repository)
        {
            _repository = repository;
        }


        // GET
        protected override DriverResult Editor(BitbucketSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_BitbucketSettings_SiteSettings",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts.BitbucketSettings.SiteSettings",
                    Model: part,
                    Prefix: Prefix)).OnGroup("BitbucketSettings");
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
            }

            _repository.Flush();

            return Editor(part, shapeHelper);
        }
    }
}