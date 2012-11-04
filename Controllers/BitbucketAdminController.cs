using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using OrchardHUN.ExternalPages.Models;
using OrchardHUN.ExternalPages.Services;

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
    }
}