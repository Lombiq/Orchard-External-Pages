using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;

namespace OrchardHUN.Bitbucket.Models
{
    public class BitbucketSettingsPart : ContentPart
    {
        private readonly LazyField<IList<RepositorySettingsRecord>> _repositories = new LazyField<IList<RepositorySettingsRecord>>();
        public LazyField<IList<RepositorySettingsRecord>> RepositoriesField { get { return _repositories; } }
        public IList<RepositorySettingsRecord> Repositories
        {
            get { return _repositories.Value; }
            set { RepositoriesField.Value = value; }
        }

        public RepositorySettingsRecord NewRepository { get; set; }

        public BitbucketSettingsPart()
        {
            NewRepository = new RepositorySettingsRecord();
        }
    }
}