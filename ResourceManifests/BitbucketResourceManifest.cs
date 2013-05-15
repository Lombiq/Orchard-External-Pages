using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.UI.Resources;

namespace OrchardHUN.ExternalPages.ResourceManifests
{
    public class BitbucketResourceManifest : IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest.DefineStyle("OrchardHUN.ExternalPages.Bitbucket").SetUrl("orchardhun-externalpages-bitbucket.css");
        }
    }
}