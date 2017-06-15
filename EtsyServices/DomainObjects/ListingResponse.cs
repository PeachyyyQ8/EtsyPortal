using System.Runtime.Serialization;

namespace EtsyServices.DomainObjects
{
    [DataContract]
    public class ListingResponse
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }
        [DataMember(Name = "results")]    
        public Listing[] Listing { get; set; }
    }
}