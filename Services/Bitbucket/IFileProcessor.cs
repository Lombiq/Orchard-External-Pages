using Orchard;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public interface IFileProcessor : IDependency
    {
        void ProcessFiles(UpdateJobContext jobContext);
    }
}
