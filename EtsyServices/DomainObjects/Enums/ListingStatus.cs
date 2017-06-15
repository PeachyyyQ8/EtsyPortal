using System;
using EtsyServices;

namespace EtsyServicer.DomainObjects.Enums
{
    public enum ListingStatus
    {
        [StringValue("draft")]
        Draft = 0,
        [StringValue("active")]
        Active = 0
    }
}