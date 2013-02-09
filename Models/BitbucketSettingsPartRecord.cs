﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;

namespace OrchardHUN.ExternalPages.Models
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketSettingsPartRecord : ContentPartRecord
    {
        public virtual int MinutesBetweenPulls { get; set; }

        public BitbucketSettingsPartRecord()
        {
            MinutesBetweenPulls = 10;
        }
    }
}