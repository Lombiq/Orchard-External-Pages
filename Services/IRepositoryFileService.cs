﻿using Orchard;
using Orchard.FileSystems.Media;

namespace OrchardHUN.ExternalPages.Services
{
    public interface IRepositoryFileService : IDependency
    {
        void SaveFile(string path, byte[] content);
        IStorageFile GetFile(string path);
        void DeleteFile(string path);
    }
}
