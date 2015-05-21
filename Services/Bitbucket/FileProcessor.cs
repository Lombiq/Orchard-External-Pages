using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Security;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Utilities;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class FileProcessor : IFileProcessor
    {
        private readonly IBitbucketApiService _apiService;
        private readonly IRepository<BitbucketRepositoryDataRecord> _repository;
        private readonly IContentManager _contentManager;
        private readonly IRepositoryFileService _fileService;
        private readonly IEncryptionService _encryptionService;
        private readonly Dictionary<string, ContentItem> _parentPagesCache = new Dictionary<string, ContentItem>();


        public FileProcessor(
            IBitbucketApiService apiService,
            IRepository<BitbucketRepositoryDataRecord> repository,
            IContentManager contentManager,
            IRepositoryFileService fileService,
            IEncryptionService encryptionService)
        {
            _apiService = apiService;
            _repository = repository;
            _contentManager = contentManager;
            _fileService = fileService;
            _encryptionService = encryptionService;
        }


        public void ProcessFiles(UpdateJobContext jobContext)
        {
            var repoData = _repository.Get(jobContext.RepositoryId);

            if (repoData == null) return;

            if (string.IsNullOrEmpty(repoData.PageContentTypeName)) repoData.PageContentTypeName = WellKnownConstants.DefaultRepoPageContentType;

            var repoSettings = new BitbucketRepositorySettings(repoData, _encryptionService);

            var urlMappings = repoData.UrlMappings();

            // Ordering so files on the top of the folder hierarchy and index files are first. This way subsequent files will be able to find 
            // their parent.
            foreach (var file in jobContext.Files.OrderBy(f => f.Path.Count(c => c == '/')).ThenBy(f => f.Path.IsIndexFilePath() ? 0 : 1))
            {
                Process(file, urlMappings, repoData, repoSettings, jobContext);
            }
        }


        private void Process(UpdateJobFile file, IEnumerable<UrlMapping> urlMappings, BitbucketRepositoryDataRecord repoData, BitbucketRepositorySettings repoSettings, UpdateJobContext jobContext)
        {
            var mapping = urlMappings.Where(urlMapping => file.Path.StartsWith(urlMapping.RepoPath)).FirstOrDefault();
            if (mapping == null) return;

            var localPath = file.Path;
            if (!String.IsNullOrEmpty(mapping.RepoPath)) localPath = localPath.Replace(mapping.RepoPath, mapping.LocalPath.Trim('/'));
            else localPath = UriHelper.Combine(mapping.LocalPath, localPath);

            if (file.Path.IsMarkdownFilePath()) ProcessPage(file, localPath, repoData, repoSettings, jobContext);
            else if (repoData.MirrorFiles) ProcessFile(file, localPath, repoData, repoSettings, jobContext);
        }


        private void ProcessFile(UpdateJobFile file, string localPath, BitbucketRepositoryDataRecord repoData, BitbucketRepositorySettings repoSettings, UpdateJobContext jobContext)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(localPath))) return;

            if (file.Type != UpdateJobfileType.Removed)
            {
                var sizeProbe = _apiService.FetchFromRepo<FolderSrcResponse>(repoSettings, UriHelper.Combine("src", jobContext.Revision.ToString(), Path.GetDirectoryName(file.Path)));
                var size = sizeProbe.Files.Where(f => f.Path == file.Path).Single().Size;
                if (size > repoData.MaximalFileSizeKB * 1024) return;
                _fileService.SaveFile(localPath, _apiService.FetchFromRepo(repoSettings, UriHelper.Combine("raw", jobContext.Revision.ToString(), file.Path)));
            }
            else
            {
                _fileService.DeleteFile(localPath);
            }
        }

        private void ProcessPage(UpdateJobFile file, string localPath, BitbucketRepositoryDataRecord repoData, BitbucketRepositorySettings repoSettings, UpdateJobContext jobContext)
        {
            localPath = localPath.Replace("Index", "").Replace(".md", "");
            if (file.Path.IsIndexFilePath()) localPath = UriHelper.Combine(localPath, "/"); // Trailing slash for index files directly fetched
            var repoBasePath = UriHelper.Combine("bitbucket.org", repoData.AccountName, repoData.Slug);
            var fullRepoFilePath = UriHelper.Combine(repoBasePath, file.Path);

            ContentItem page = null;

            if (file.Type != UpdateJobfileType.Removed)
            {
                var src = _apiService.FetchFromRepo<FileSrcResponse>(repoSettings, UriHelper.Combine("src", jobContext.Revision.ToString(), file.Path));

                if (file.Type != UpdateJobfileType.Added) page = FetchPage(repoData.PageContentTypeName, fullRepoFilePath);

                var isNew = page == null;

                if (isNew) page = _contentManager.New(repoData.PageContentTypeName);

                var pagePart = page.As<MarkdownPagePart>();
                // We're updating the path for the repo file in the DB if it doesn't match the one that comes from the repo.
                // This is needed to overcome the issue caused by the case-sensitive URLs on BitBucket, when the casing of any character changes in the file path.
                if (pagePart.RepoPath != fullRepoFilePath)
                {
                    var autoroutePart = page.As<AutoroutePart>();
                    autoroutePart.CustomPattern = localPath;
                    autoroutePart.UseCustomPattern = true;
                    autoroutePart.DisplayAlias = localPath;
                    page.As<MarkdownPagePart>().RepoPath = fullRepoFilePath;
                }

                // Searching for the (first) title in the markdown text. Doesn't work if the text is less than or equal to a single line.
                var lines = Regex.Split(src.Data, "\r\n|\r|\n").ToList();
                int i = 1;
                var titleFound = false;
                var title = string.Empty;
                while (!titleFound && i < lines.Count)
                {
                    // If this line consists of just equals signs, the above line is a H1
                    if (Regex.IsMatch(lines[i], "^[=]+$"))
                    {
                        title = lines[i - 1];
                        titleFound = true;
                        lines.RemoveAt(i - 1);
                        lines.RemoveAt(i);
                    }
                    // Or if it starts with a single hashmark
                    else if (lines[i - 1].StartsWith("#"))
                    {
                        title = lines[i - 1].Substring(1).Trim();
                        titleFound = true;
                        lines.RemoveAt(i - 1);
                    }

                    i++;
                }
                page.As<TitlePart>().Title = Regex.Replace(title, @"\\([\`*_{}[\]()#+-.!])", match => match.Groups[1].Value);

                // Cleaning leading line breaks
                while (lines.Count > 0 && string.IsNullOrEmpty(lines[0].Trim())) lines.RemoveAt(0);

                page.As<BodyPart>().Text = string.Join(Environment.NewLine, lines);

                var parent = FindParent(repoData.PageContentTypeName, repoBasePath, fullRepoFilePath);
                if (parent != null) page.As<CommonPart>().Container = parent;

                // This is needed after the title is set, because slug generation needs it
                if (isNew) _contentManager.Create(page);

                // This is needed so published handlers can run
                _contentManager.Unpublish(page);
                _contentManager.Publish(page);
            }
            else
            {
                page = FetchPage(repoData.PageContentTypeName, fullRepoFilePath);

                if (page == null) return;

                _contentManager.Remove(page);
            }
        }

        private ContentItem FetchPage(string pageContentTypeName, string fullRepoFilePath)
        {
            return _contentManager
                        .Query(pageContentTypeName)
                        .Where<MarkdownPagePartRecord>(record => record.RepoPath == fullRepoFilePath)
                        .Slice(0, 1)
                        .SingleOrDefault();
        }

        private ContentItem FindParent(string pageContentTypeName, string repoBasePath, string fullRepoFilePath)
        {
            // Cutting off file name
            var fullRepoFolderPath = fullRepoFilePath.Substring(0, fullRepoFilePath.LastIndexOf("/"));

            if (fullRepoFolderPath == repoBasePath) return null; // We've reached the root.

            // Jumping up one level if the file is an Index itself (it can have a parent too).
            if (fullRepoFilePath.IsIndexFilePath()) fullRepoFolderPath = fullRepoFolderPath.Substring(0, fullRepoFolderPath.LastIndexOf("/"));

            while (true)
            {
                if (fullRepoFolderPath.Replace("/", string.Empty) == repoBasePath.Replace("/", string.Empty)) return null; // This should terminate at least.

                var parentPath = UriHelper.Combine(fullRepoFolderPath, "Index.md");
                if (_parentPagesCache.ContainsKey(parentPath)) return _parentPagesCache[parentPath];

                var parent = _contentManager
                    .Query(pageContentTypeName)
                    .Where<MarkdownPagePartRecord>(record => record.RepoPath == parentPath)
                    .Slice(0, 1)
                    .SingleOrDefault();
                if (parent != null)
                {
                    _parentPagesCache[parentPath] = parent;
                    return parent;
                }

                // Jumping up one level
                fullRepoFolderPath = fullRepoFolderPath.Substring(0, fullRepoFolderPath.LastIndexOf("/"));
            }
        }
    }
}