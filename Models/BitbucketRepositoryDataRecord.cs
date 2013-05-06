using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Data.Conventions;
using Orchard.Environment.Extensions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketRepositoryDataRecord
    {
        public virtual int Id { get; set; }
        public virtual string AccountName { get; set; }
        public virtual string Slug { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual bool MirrorFiles { get; set; }
        public virtual int MaximalFileSizeKB { get; set; }
        

        [StringLengthMax]
        public virtual string UrlMappingsDefinition { get; set; }

        public virtual string LastCheckedNode { get; set; }
        public virtual int LastCheckedRevision { get; set; }
        public virtual string LastProcessedNode { get; set; }
        public virtual int LastProcessedRevision { get; set; }

        public BitbucketRepositoryDataRecord()
        {
            MaximalFileSizeKB = 1024;
            LastCheckedRevision = -1;
            LastProcessedRevision = -1;
        }
    }

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public static class RepositorySettingsRecordExtensions
    {
        public static IEnumerable<UrlMapping> UrlMappings(this BitbucketRepositoryDataRecord settings)
        {
            if (String.IsNullOrEmpty(settings.UrlMappingsDefinition)) return Enumerable.Empty<UrlMapping>();

            var mappings = new List<UrlMapping>();
            foreach (var mappingLine in Regex.Split(settings.UrlMappingsDefinition.Trim(), "\r\n|\r|\n"))
            {
                if (!String.IsNullOrEmpty(mappingLine))
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
            return !String.IsNullOrEmpty(settings.LastCheckedNode);
        }

        public static bool WasProcessed(this BitbucketRepositoryDataRecord settings)
        {
            return !String.IsNullOrEmpty(settings.LastProcessedNode);
        }
    }

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class UrlMapping
    {
        public string LocalPath { get; set; }
        public string RepoPath { get; set; }
    }
}