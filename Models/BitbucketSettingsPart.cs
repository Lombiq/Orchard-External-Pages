using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;

namespace OrchardHUN.ExternalPages.Models
{
    public class BitbucketSettingsPart : ContentPart
    {
        private readonly LazyField<IList<BitbucketRepositorySettingsRecord>> _repositories = new LazyField<IList<BitbucketRepositorySettingsRecord>>();
        public LazyField<IList<BitbucketRepositorySettingsRecord>> RepositoriesField { get { return _repositories; } }
        public IList<BitbucketRepositorySettingsRecord> Repositories
        {
            get { return _repositories.Value; }
            set { RepositoriesField.Value = value; }
        }

        public BitbucketRepositorySettingsRecord NewRepository { get; set; }

        public BitbucketSettingsPart()
        {
            NewRepository = new BitbucketRepositorySettingsRecord();
        }
    }
}