using System;
using System.Web.Mvc;
using Orchard.Environment.Extensions;
using Orchard.Exceptions;
using Orchard.Localization;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Services.Bitbucket;

namespace OrchardHUN.ExternalPages.Controllers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    [Admin]
    public class BitbucketAdminController : Controller
    {
        private readonly IBitbucketService _bitbucketService;
        private readonly INotifier _notifier;

        public Localizer T { get; set; }


        public BitbucketAdminController(IBitbucketService bitbucketService, INotifier notifier)
        {
            _bitbucketService = bitbucketService;
            _notifier = notifier;

            T = NullLocalizer.Instance;
        }


        [HttpPost]
        public ActionResult DeleteRepository(int id, string returnUrl)
        {
            var repoDataRepository = _bitbucketService.RepositoryDataRepository;

            var record = repoDataRepository.Get(id);
            if (record != null) repoDataRepository.Delete(record);

            _notifier.Information(T("Repository deleted."));

            return Redirect(returnUrl);
        }

        [HttpPost]
        public ActionResult RepopulateRepository(int id, string returnUrl)
        {
            var repoData = _bitbucketService.RepositoryDataRepository.Get(id);

            if (repoData == null) return Redirect(returnUrl);

            try
            {
                _bitbucketService.Repopulate(id);
                _notifier.Information(T("Repopulation from the repository scheduled."));
            }
            catch (Exception ex)
            {
                if (ex.IsFatal()) throw;

                _notifier.Error(T("Repopulation failed with the following exception: {0}", ex.Message));
            }

            return Redirect(returnUrl);
        }
    }
}