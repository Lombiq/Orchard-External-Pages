using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Security;
using OrchardHUN.ExternalPages.Services.Bitbucket;

namespace OrchardHUN.ExternalPages.Models
{
    public class BitbucketRepositorySettings : IBitbucketRepositorySettings
    {
        private readonly BitbucketRepositoryDataRecord _record;
        private readonly IEncryptionService _encryptionService;


        public string AccountName
        {
            get { return _record.AccountName; }
        }

        public string Slug
        {
            get { return _record.Slug; }
        }

        public string Username
        {
            get { return _record.Username; }
        }

        public string Password
        {
            get { return _record.GetDecodedPassword(_encryptionService); }
        }


        public BitbucketRepositorySettings(BitbucketRepositoryDataRecord repositoryData, IEncryptionService encryptionService)
        {
            _record = repositoryData;
            _encryptionService = encryptionService;
        }
    }
}