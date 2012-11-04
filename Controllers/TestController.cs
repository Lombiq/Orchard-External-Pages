using System.Web.Mvc;
using Orchard.Settings;
using OrchardHUN.ExternalPages.Services;

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