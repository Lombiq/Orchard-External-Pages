using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.Settings;
using OrchardHUN.ExternalPages.Services;
using RestSharp;
using Orchard.ContentManagement;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Controllers
{
    public class TestController : Controller
    {
        private readonly ISiteService _siteService;
        private readonly IBitbucketService _bitbucketService;


        public TestController(ISiteService siteService, IBitbucketService bitbucketService)
        {
            _siteService = siteService;
            _bitbucketService = bitbucketService;
        }


        public void Index()
        {
            _bitbucketService.CheckChangesets(1);
        }
    }
}