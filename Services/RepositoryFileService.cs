using System;
using System.Text;
using System.Web.Routing;
using Orchard.Alias;
using Orchard.Exceptions;
using Orchard.FileSystems.Media;

namespace OrchardHUN.ExternalPages.Services
{
    public class RepositoryFileService : IRepositoryFileService
    {
        private const string RootFolder = "ExternalPages/";

        private readonly IStorageProvider _storageProvider;
        private readonly IAliasService _aliasService;


        public RepositoryFileService(IStorageProvider storageProvider, IAliasService aliasService)
        {
            _storageProvider = storageProvider;
            _aliasService = aliasService;
        }


        public void SaveFile(string path, byte[] content)
        {
            var file = GetFile(path);
            if (file == null) file = _storageProvider.CreateFile(PathToStoragePath(path));

            using (var stream = file.OpenWrite())
            {
                stream.Write(content, 0, content.Length);
            }

            var fileRoute = new RouteValueDictionary
                            {
                                {"area", "OrchardHUN.ExternalPages"},
                                {"controller", "RepositoryFile"},
                                {"action", "File"},
                                {"path", path}
                            };

            _aliasService.Set(path, fileRoute, "");
        }

        public IStorageFile GetFile(string path)
        {
            if (!_storageProvider.FileExists(PathToStoragePath(path))) return null;
            return _storageProvider.GetFile(PathToStoragePath(path));
        }

        public void DeleteFile(string path)
        {
            var file = GetFile(path);
            if (file == null) return;

            _storageProvider.DeleteFile(PathToStoragePath(path));
        }


        private string PathToStoragePath(string path)
        {
            return RootFolder + path;
        }
    }
}