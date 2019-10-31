using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public class CommitsResponse
    {
        /// <remarks>
        /// Commits are ordered by date, so the first one is the oldest
        /// </remarks>
        public List<Commit> Values { get; set; }
    }

    public class Commit
    {
        public string Hash { get; set; }
        public DateTime Date { get; set; }
    }

    public class DiffStat
    {
        // This doesn't handle paging but pages have 500 items by default and that should be plenty.
        public List<DiffStatValue> Values { get; set; }
    }

    public class DiffStatValue
    {
        public string Status { get; set; }
        public DiffStatFile Old { get; set; }
        public DiffStatFile New { get; set; }
    }

    public class DiffStatFile
    {
        public string Path { get; set; }
    }

    public class Meta
    {
        public int Size { get; set; }
    }

    public class FolderSrcResponse
    {
        public string Node { get; set; }
        public List<string> Directories { get; set; }
        public List<FolderSrcFile> Files { get; set; }
    }

    public class FolderSrcFile
    {
        public int Size { get; set; }
        public string Path { get; set; }
        public DateTime UtcTimestamp { get; set; }
    }
}