using System;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization;
using RestSharp.Extensions;

namespace EtsyServices.DomainObjects
{
    [DataContract]
    public class  ListingImage
    {
        [DataMember(Name = "listing_image_id")]
        public string ID { get; set; }
        
        public Image ImageFile { get; set; }

        [DataMember(Name = "rank")]
        public int Rank { get; set; }

        [DataMember(Name = "overwrite")]
        public bool Overwrite { get; set; }

        [DataMember(Name = "is_watermarked")]
        public bool IsWatermarked { get; set; }

        public string ImagePath { get; set; }
    }
}