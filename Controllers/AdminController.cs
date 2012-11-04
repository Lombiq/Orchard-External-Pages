using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.Data;
using Orchard.Localization;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Controllers
{
    [Admin]
    public class AdminController : Controller
    {
        private readonly IRepository<BitbucketRepositorySettingsRecord> _repository;
        private readonly INotifier _notifier;

        public Localizer T { get; set; }


        public AdminController(IRepository<BitbucketRepositorySettingsRecord> repository, INotifier notifier)
        {
            _repository = repository;
            _notifier = notifier;

            T = NullLocalizer.Instance;
        }


        [HttpPost]
        public ActionResult DeleteRepository(int id, string returnUrl)
        {
            var record = _repository.Get(id);
            if (record != null) _repository.Delete(record);

            _notifier.Information(T("Repository deleted."));

            return Redirect(returnUrl);
        }
    }
}