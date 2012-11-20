using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public interface IFileProcessor : IDependency
    {
        void ProcessFiles(UpdateJobContext jobContext);
    }
}
