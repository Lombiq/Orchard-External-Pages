using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Routes;

namespace OrchardHUN.ExternalPages.Routes
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketRoutes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (var routeDescriptor in GetRoutes())
                routes.Add(routeDescriptor);
        }

        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[]
            {
                new RouteDescriptor
                {
                    Name = "BitbucketAdminRoute",
                    Route = new Route(
                        "Admin/OrchardHUN.ExternalPages/Bitbucket/{action}",
                        new RouteValueDictionary
                        {
                            {"area", "OrchardHUN.ExternalPages"},
                            {"controller", "BitbucketAdmin"}
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary
                        {
                            {"area", "OrchardHUN.ExternalPages"}
                        },
                        new MvcRouteHandler())
                }
            };
        }
    }
}