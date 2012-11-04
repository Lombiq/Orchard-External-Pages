using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Environment.Extensions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
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