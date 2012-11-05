using System;
using System.Web.Mvc;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Services;
using Orchard.Exceptions;

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
            var settingsRepository = _bitbucketService.SettingsRepository;

            var record = settingsRepository.Get(id);
            if (record != null) settingsRepository.Delete(record);

            _notifier.Information(T("Repository deleted."));

            return Redirect(returnUrl);
        }

        [HttpPost]
        public ActionResult PopulateRepository(int id, string returnUrl)
        {
            var settings = _bitbucketService.SettingsRepository.Get(id);

            if (settings == null) return Redirect(returnUrl);

            try
            {
                _bitbucketService.Populate(id);
                _notifier.Information(T("Initial population from the repository scheduled."));
            }
            catch (Exception ex)
            {
                if (ex.IsFatal()) throw;

                _notifier.Error(T("Initial population failed with the following exception: {0}", ex.Message));
            }

            return Redirect(returnUrl);
        }
    }
}