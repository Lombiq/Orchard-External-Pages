using System.Collections.Generic;
using System.Diagnostics;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public class UpdateJobContext
    {
        public int RepositoryId { get; private set; }
        public string Node { get; private set; }
        public IEnumerable<UpdateJobFile> Files { get; private set; }
        public bool IsRepopulation { get; private set; }

        public UpdateJobContext(int repositoryId, string node, IEnumerable<UpdateJobFile> files, bool isRepopulation)
        {
            RepositoryId = repositoryId;
            Node = node;
            Files = files;
            IsRepopulation = isRepopulation;
        }
    }

    [DebuggerDisplay("{Type}: {Path}")]
    public class UpdateJobFile
    {
        public string Path { get; private set; }
        public UpdateJobfileType Type { get; private set; }

        public UpdateJobFile(string path, UpdateJobfileType type)
        {
            Path = path;
            Type = type;
        }
    }

    public enum UpdateJobfileType
    {
        Added,
        Modified,
        Removed,
        // Needed for when Repopulate() is called: there we don't know whether a file was just created or is modified
        AddedOrModified
    }
}