using System.Runtime.Serialization;

namespace EtsyServices.DomainObjects
{
    [DataContract]
    public class DigitalFile
    {
        public string Path { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "rank")]
        public int Rank { get; set; }
    }
}