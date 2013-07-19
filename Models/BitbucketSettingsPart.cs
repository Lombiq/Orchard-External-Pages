using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Environment.Extensions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPart : ContentPart<BitbucketSettingsPartRecord> 
    {
        public int MinutesBetweenPulls
        {
            get { return Record.MinutesBetweenPulls; }
            set { Record.MinutesBetweenPulls = value; }
        }

        private readonly LazyField<IList<BitbucketRepositoryDataRecord>> _repositories = new LazyField<IList<BitbucketRepositoryDataRecord>>();
        internal LazyField<IList<BitbucketRepositoryDataRecord>> RepositoriesField { get { return _repositories; } }
        public IList<BitbucketRepositoryDataRecord> Repositories
        {
            get { return _repositories.Value; }
            set { RepositoriesField.Value = value; }
        }

        public BitbucketRepositoryDataRecord NewRepository { get; set; }

        public BitbucketSettingsPart()
        {
            NewRepository = new BitbucketRepositoryDataRecord();
        }
    }
}