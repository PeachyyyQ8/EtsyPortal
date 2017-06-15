using EtsyServicer.DomainObjects;
using EtsyServices.DomainObjects;

namespace EtsyServices
{
    public interface IEtsyService
    {
        Listing CreateListing(Listing listing);
        string GetPermissionScopes();
        void Configure(string[] permissions);
        Listing GetListingByID(int listingID);
    }
}