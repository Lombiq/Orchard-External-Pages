using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Services;

namespace OrchardHUN.ExternalPages.Controllers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class HomeController : Controller
    {
        private readonly IBitbucketService _bitbucketService;

        public HomeController(IBitbucketService bitbucketService)
        {
            _bitbucketService = bitbucketService;
        }

        public void Index()
        {
            _bitbucketService.ProcessNextPendingChangeset();
        }
    }
}