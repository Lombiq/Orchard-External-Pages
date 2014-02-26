using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Environment.Extensions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPart : ContentPart 
    {
        public int MinutesBetweenPulls
        {
            get { return this.Retrieve(x => x.MinutesBetweenPulls, 10); }
            set { this.Store(x => x.MinutesBetweenPulls, value); }
        }

        private readonly LazyField<IList<BitbucketRepositoryDataRecord>> _repositories = new LazyField<IList<BitbucketRepositoryDataRecord>>();
        internal LazyField<IList<BitbucketRepositoryDataRecord>> RepositoriesField { get { return _repositories; } }
        public IList<BitbucketRepositoryDataRecord> Repositories
        {
            get { return _repositories.Value; }
            set { RepositoriesField.Value = value; }
        }

        public BitbucketRepositoryDataRecord NewRepository { get; set; }

        /// <summary>
        /// This is to check whether the EP settings page is being posted or any other editor group from site settings.
        /// </summary>
        public bool IsBeingSaved { get; set; }


        public BitbucketSettingsPart()
        {
            NewRepository = new BitbucketRepositoryDataRecord();
        }
    }
}