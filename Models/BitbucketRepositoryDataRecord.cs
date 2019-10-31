using Orchard.Data.Conventions;
using Orchard.Environment.Extensions;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketRepositoryDataRecord
    {
        public virtual int Id { get; set; }
        public virtual string AccountName { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Username { get; set; }
        [DataType(DataType.Password), StringLengthMax]
        public virtual string Password { get; set; }
        public virtual string PageContentTypeName { get; set; }
        public virtual bool MirrorFiles { get; set; }
        public virtual int MaximalFileSizeKB { get; set; }
        [StringLengthMax]
        public virtual string UrlMappingsDefinition { get; set; }
        public virtual string LastCheckedNode { get; set; }
        public virtual string LastProcessedNode { get; set; }
        public virtual int LastProcessedRevision { get; set; }


        public BitbucketRepositoryDataRecord()
        {
            PageContentTypeName = WellKnownConstants.DefaultRepoPageContentType;
            MaximalFileSizeKB = 1024;
            LastProcessedRevision = -1;
        }
    }

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public static class RepositorySettingsRecordExtensions
    {
        public static IEnumerable<UrlMapping> UrlMappings(this BitbucketRepositoryDataRecord settings)
        {
            if (string.IsNullOrEmpty(settings.UrlMappingsDefinition)) return Enumerable.Empty<UrlMapping>();

            var mappings = new List<UrlMapping>();
            foreach (var mappingLine in Regex.Split(settings.UrlMappingsDefinition.Trim(), "\r\n|\r|\n"))
            {
                if (!string.IsNullOrEmpty(mappingLine))
                {
                    var sides = mappingLine.Trim().Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (sides.Length == 2)
                    {
                        var mapping = new UrlMapping();
                        mapping.RepoPath = sides.First().Trim().Trim('/');
                        mapping.LocalPath = sides.Last().Trim().Trim('/');
                        if (!mapping.RepoPath.IsMarkdownFilePath() || mapping.RepoPath.IsIndexFilePath()) mapping.LocalPath += "/";
                        mappings.Add(mapping);
                    }
                }
            }

            return mappings;
        }

        public static bool WasChecked(this BitbucketRepositoryDataRecord settings)
        {
            return !string.IsNullOrEmpty(settings.LastCheckedNode);
        }

        public static bool WasProcessed(this BitbucketRepositoryDataRecord settings)
        {
            return !string.IsNullOrEmpty(settings.LastProcessedNode);
        }

        public static string SetPasswordEncrypted(this BitbucketRepositoryDataRecord settings, IEncryptionService encryptionService, string plainPassword)
        {
            if (plainPassword == null) plainPassword = string.Empty;
            settings.Password = Convert.ToBase64String(encryptionService.Encode(Encoding.UTF8.GetBytes(plainPassword)));
            return settings.Password;
        }

        public static string GetDecodedPassword(this BitbucketRepositoryDataRecord settings, IEncryptionService encryptionService)
        {
            return Encoding.UTF8.GetString(encryptionService.Decode(Convert.FromBase64String(settings.Password)));
        }
    }

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class UrlMapping
    {
        public string LocalPath { get; set; }
        public string RepoPath { get; set; }
    }
}