using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.ContentManagement;

namespace OrchardHUN.Bitbucket.Models
{
    public interface IRepositorySettings
    {
        string AccountName { get; }
        string Slug { get; }
        string Username { get; }
        string Password { get; }
        IEnumerable<UrlMapping> UrlMappings { get; }
    }
}
