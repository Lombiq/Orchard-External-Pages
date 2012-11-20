using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.FileSystems.Media;
using Orchard.Exceptions;
using Piedone.HelpfulLibraries.Libraries.Utilities;
using OrchardHUN.ExternalPages.Services;
using System.IO;

namespace OrchardHUN.ExternalPages.Controllers
{
    public class RepositoryFileController : Controller
    {
        private readonly IRepositoryFileService _fileService;


        public RepositoryFileController(IRepositoryFileService fileService)
        {
            _fileService = fileService;
        }


        public ActionResult File(string path)
        {
            var file = _fileService.GetFile(path);
            if (file == null) return HttpNotFound();
            // No need to dispose the stream here
            return File(file.OpenRead(), MimeAssistant.GetMimeType(path));
        }
    }
}