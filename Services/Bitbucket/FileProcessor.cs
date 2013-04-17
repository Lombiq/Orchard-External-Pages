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
        private readonly Dictionary<string, ContentItem> _parentPagesCache = new Dictionary<string, ContentItem>();


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

            // Ordering so files on the top of the folder hierarchy and index files are first. This way subsequent files will be able to find 
            // their parent.
            foreach (var file in jobContext.Files.OrderBy(f => f.Path.Count(c => c == '/')).ThenBy(f => f.Path.EndsWith("Index.md") ? 0 : 1))
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
            var repoBasePath = UriHelper.Combine("bitbucket.org", repoData.AccountName, repoData.Slug);
            var fullRepoFilePath = UriHelper.Combine(repoBasePath, file.Path);

            ContentItem page = null;

            if (file.Type != UpdateJobfileType.Removed)
            {
                var src = ApiHelper.GetResponse<FileSrcResponse>(repoData, UriHelper.Combine("src", jobContext.Revision.ToString(), file.Path));

                if (file.Type != UpdateJobfileType.Added) page = FetchPage(fullRepoFilePath);

                var isNew = page == null;

                if (isNew)
                {
                    page = _contentManager.New(WellKnownConstants.RepoPageContentType);

                    var autoroutePart = page.As<AutoroutePart>();
                    autoroutePart.CustomPattern = localPath;
                    autoroutePart.UseCustomPattern = true;
                    autoroutePart.DisplayAlias = localPath;
                    page.As<MarkdownPagePart>().RepoPath = fullRepoFilePath;
                }

                // Searching for the (first) title in the markdown text
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

                var parent = FindParent(repoBasePath, fullRepoFilePath);
                if (parent != null) page.As<CommonPart>().Container = parent;

                // This is needed after the title is set, because slug generation needs it
                if (isNew) _contentManager.Create(page);

                _contentManager.Publish(page);
                _contentManager.Flush();
            }
            else
            {
                page = FetchPage(fullRepoFilePath);

                if (page == null) return;

                _contentManager.Remove(page);
            }
        }

        private ContentItem FetchPage(string fullRepoFilePath)
        {
            return _contentManager
                        .Query(WellKnownConstants.RepoPageContentType)
                        .Where<MarkdownPagePartRecord>(record => record.RepoPath == fullRepoFilePath)
                        .List()
                        .SingleOrDefault();
        }

        private ContentItem FindParent(string repoBasePath, string fullRepoFilePath)
        {
            // Cutting off file name
            var fullRepoFolderPath = fullRepoFilePath.Substring(0, fullRepoFilePath.LastIndexOf("/"));

            // Jumping up one level if the file is an Index itself (it can have a parent too).
            if (fullRepoFilePath.EndsWith("Index.md")) fullRepoFolderPath = fullRepoFolderPath.Substring(0, fullRepoFolderPath.LastIndexOf("/"));

            while (true)
            {
                if (fullRepoFolderPath.Replace("/", string.Empty) == repoBasePath.Replace("/", string.Empty)) return null; // This should terminate at least.

                var parentPath = UriHelper.Combine(fullRepoFolderPath, "Index.md");
                if (_parentPagesCache.ContainsKey(parentPath)) return _parentPagesCache[parentPath];

                var parent = _contentManager
                    .Query(WellKnownConstants.RepoPageContentType)
                    .Where<MarkdownPagePartRecord>(record => record.RepoPath == parentPath)
                    .List()
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