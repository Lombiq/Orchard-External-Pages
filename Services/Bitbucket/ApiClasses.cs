using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public class ChangesetsResponse
    {
        /// <remarks>
        /// Changesets are ordered by date, so the first one is the oldest
        /// </remarks>
        public List<Changeset> Changesets { get; set; }
    }

    public class Changeset
    {
        public string Branch { get; set; }
        public int Revision { get; set; }
        public string Node { get; set; }
        public DateTime UtcTimestamp { get; set; }
        public List<ChangesetFile> Files { get; set; }
    }

    public class ChangesetFile
    {
        public string Type { get; set; }
        public string File { get; set; }
    }

    public class FileSrcResponse
    {
        public string Node { get; set; }
        public string Data { get; set; }
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