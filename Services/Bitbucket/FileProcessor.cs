using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Libraries.Utilities;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class FileProcessor : IFileProcessor
    {
        private readonly IRepository<BitbucketRepositoryDataRecord> _repository;
        private readonly IContentManager _contentManager;
        private readonly IRepositoryFileService _fileService;


        public FileProcessor(
            IRepository<BitbucketRepositoryDataRecord> repository,
            IContentManager contentManager,
            IRepositoryFileService fileService)
        {
            _repository = repository;
            _contentManager = contentManager;
            _fileService = fileService;
        }


        public void ProcessFiles(UpdateJobContext jobContext)
        {
            var repoData = _repository.Get(jobContext.RepositoryId);
            if (repoData == null) return;

            var urlMappings = repoData.UrlMappings();

            foreach (var file in jobContext.Files)
            {
                Process(file, urlMappings, repoData, jobContext);
            }
        }


        private void Process(UpdateJobFile file, IEnumerable<UrlMapping> urlMappings, BitbucketRepositoryDataRecord repoData, UpdateJobContext jobContext)
        {
            var mapping = urlMappings.Where(urlMapping => file.Path.StartsWith(urlMapping.RepoPath)).FirstOrDefault();
            if (mapping == null) return;

            var localPath = file.Path;
            if (!String.IsNullOrEmpty(mapping.RepoPath)) localPath = localPath.Replace(mapping.RepoPath, mapping.LocalPath.Trim('/'));
            else localPath = UriHelper.Combine(mapping.LocalPath, localPath);

            if (file.Path.IsMarkdownFilePath()) ProcessPage(file, localPath, repoData, jobContext);
            else if (repoData.MirrorFiles) ProcessFile(file, localPath, repoData, jobContext);
        }


        private void ProcessFile(UpdateJobFile file, string localPath, BitbucketRepositoryDataRecord repoData, UpdateJobContext jobContext)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(localPath))) return;

            if (file.Type != UpdateJobfileType.Removed)
            {
                var sizeProbe = ApiHelper.GetResponse<FolderSrcResponse>(repoData, UriHelper.Combine("src", jobContext.Revision.ToString(), Path.GetDirectoryName(file.Path)));
                var size = sizeProbe.Files.Where(f => f.Path == file.Path).Single().Size;
                if (size > repoData.MaximalFileSizeKB * 1024) return;
                _fileService.SaveFile(localPath, ApiHelper.GetResponse(repoData, UriHelper.Combine("raw", jobContext.Revision.ToString(), file.Path)));
            }
            else
            {
                _fileService.DeleteFile(localPath);
            }
        }

        private void ProcessPage(UpdateJobFile file, string localPath, BitbucketRepositoryDataRecord repoData, UpdateJobContext jobContext)
        {
            localPath = localPath.Replace("Index", "").Replace(".md", "");

            ContentItem page = null;

            if (file.Type != UpdateJobfileType.Removed)
            {
                var src = ApiHelper.GetResponse<FileSrcResponse>(repoData, UriHelper.Combine("src", jobContext.Revision.ToString(), file.Path));

                if (file.Type != UpdateJobfileType.Added) page = FetchPage(file.Path);

                var isNew = page == null;

                if (isNew)
                {
                    page = _contentManager.New(WellKnownConstants.RepoPageContentType);

                    var autoroutePart = page.As<AutoroutePart>();
                    autoroutePart.CustomPattern = localPath;
                    autoroutePart.UseCustomPattern = true;
                    autoroutePart.DisplayAlias = localPath;
                    page.As<MarkdownPagePart>().RepoPath = file.Path;
                }

                page.As<MarkdownPagePart>().Text = src.Data;

                // Searching for the (first) title in the markdown text
                var lines = Regex.Split(src.Data, "\r\n|\r|\n");
                int i = 1;
                var titleFound = false;
                while (!titleFound && i < lines.Length)
                {
                    // If this line consists of just equals signs, the above line is a H1
                    if (Regex.IsMatch(lines[i], "^[=]+$"))
                    {
                        page.As<TitlePart>().Title = lines[i - 1];
                        titleFound = true;
                    }
                    // Or if it starts with a single hashmark
                    else if (lines[i - 1].StartsWith("#"))
                    {
                        page.As<TitlePart>().Title = lines[i - 1].Substring(1).Trim();
                        titleFound = true;
                    }

                    i++;
                }

                // This is needed after the title is set, because slug generation needs it
                if (isNew) _contentManager.Create(page);

                _contentManager.Publish(page);
                _contentManager.Flush();
            }
            else
            {
                page = FetchPage(file.Path);

                if (page == null) return;

                _contentManager.Remove(page);
            }
        }

        private ContentItem FetchPage(string repoPath)
        {
            return _contentManager
                        .Query(WellKnownConstants.RepoPageContentType)
                        .Where<MarkdownPagePartRecord>(record => record.RepoPath == repoPath)
                        .List()
                        .FirstOrDefault();
        }
    }
}