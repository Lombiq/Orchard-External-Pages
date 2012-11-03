using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Orchard.Data.Conventions;

namespace OrchardHUN.Bitbucket.Models
{
    public class RepositorySettingsRecord
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

        public virtual string LastNode { get; set; }

        public RepositorySettingsRecord()
        {
            MaximalFileSizeKB = 1024;
        }
    }

    public static class RepositorySettingsRecordExtensions
    {
        public static IEnumerable<UrlMapping> UrlMappings(this RepositorySettingsRecord settings)
        {
            if (String.IsNullOrEmpty(settings.UrlMappingsDefinition)) return Enumerable.Empty<UrlMapping>();

            var mappings = new List<UrlMapping>();
            foreach (var mappingLine in settings.UrlMappingsDefinition.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!String.IsNullOrEmpty(mappingLine))
                {
                    var sides = mappingLine.Trim().Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (sides.Length == 2)
                    {
                        var mapping = new UrlMapping();
                        mapping.RepoPath = sides.First().Trim().Trim('/');
                        mapping.LocalPath = sides.Last().Trim().Trim('/');
                        mappings.Add(mapping);
                    }
                }
            }
            return mappings;
        }
    }

    public class UrlMapping
    {
        public string LocalPath { get; set; }
        public string RepoPath { get; set; }
    }
}