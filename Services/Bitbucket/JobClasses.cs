using System.Collections.Generic;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public class UpdateJobContext
    {
        public int RepositoryId { get; private set; }
        public string Node { get; private set; }
        public int Revision { get; private set; }
        public IEnumerable<UpdateJobFile> Files { get; private set; }
        public bool IsRepopulation { get; private set; }

        public UpdateJobContext(int repositoryId, string node, int revision, IEnumerable<UpdateJobFile> files, bool isRepopulation)
        {
            RepositoryId = repositoryId;
            Node = node;
            Revision = revision;
            Files = files;
            IsRepopulation = isRepopulation;
        }
    }

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
        AddedOrModified // Needed for when Repopulate() is called: there we don't know wether a file was just created or is modified
    }
}