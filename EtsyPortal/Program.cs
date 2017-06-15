using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EtsyServicer.DomainObjects;
using EtsyServicer.DomainObjects.Enums;
using EtsyServices;
using EtsyServices.DomainObjects;
using StructureMap;

namespace EtsyPortal
{
    class Program
    {
        private static IEtsyService _etsyService;

        static void Main(string[] args)
        {
            try
            {
                var container = new Container(new DependencyRegistry());

                _etsyService = container.GetInstance<IEtsyService>();


                var workingDirectory = SettingsHelper.GetAppSetting("workingDirectory");

                if (workingDirectory.IsNullOrEmpty())
                {
                    Console.Write("Enter working directory: ");
                    SettingsHelper.SetAppSetting("workingDirectory", Console.ReadLine());
                    workingDirectory = SettingsHelper.GetAppSetting("workingDirectory");
                }


                //string startPath = workingDirectory + "Files";
                //string zipPath = workingDirectory + args[0] + ".zip";

                //ZipFile.CreateFromDirectory(startPath, zipPath);
                
                if (args[0].IsNullOrEmpty() || args[1].IsNullOrEmpty() || args[2].IsNullOrEmpty())
                {
                    throw new InvalidDataException("You must enter command line arguments in the following order:  Title, Price, IsCustomizable, Standard Tags, Additional Tags");
                }
                decimal price;
                bool isCustomizable;
                Tags tags;
                
                _etsyService.Configure(new[] { "listings_w", "listings_r" });
                Listing listing = new Listing();
                listing.Title = args[0] + ListingResources.ResourceManager.GetString("StandardTitle");
                listing.Description = listing.Title + "\r\n\r\n" + ListingResources.ResourceManager.GetString("StandardDescription");
                listing.State = ListingStatus.Draft;
                listing.Quantity = "999";
                listing.Price = decimal.TryParse(args[1], out price) ? price : throw new InvalidDataException("Price must be a decimal value.");
                listing.IsSupply = true;
                listing.CategoryId = 69150433;
                listing.WhenMade = "2010_2017";
                listing.WhoMade = "i_did";
                listing.IsCustomizable = bool.TryParse(args[2], out isCustomizable) && isCustomizable;
                listing.IsDigital = true;
                listing.ShippingTemplateId = 30116314577;
                listing.Image = FindWatermarkedImage(workingDirectory);

                if (!args[3].IsNullOrEmpty())
                {
                    args[3] = args[3].ToUpper();
                }
                if (Enum.TryParse(args[3], out tags))
                {
                    var standardTagString = string.Empty;
                    var props = typeof(Tags).GetProperties();
                    var property = (from p in props
                        where p.Name == tags.ToString()
                        select p).First();

                   object[] attrs = property.GetCustomAttributes(true);
                        foreach (var a in attrs)
                        {
                            var attr = a as StringValueAttribute;
                            if (attr != null)
                            {
                                standardTagString = attr.Value;
                            }
                        }
                    
                    List<string> standardTagArray = standardTagString.Split(',').ToList();
                    if (args.Length == 5)
                    {
                        var additionalTags = args[4].Split(',').ToList();
                        standardTagArray.AddRange(additionalTags);
                        listing.Tags = standardTagArray.ToArray();
                    }
                    
                }
                
                var etsyListing = _etsyService.CreateListing(listing);

            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        private static string FindWatermarkedImage(string workingDirectory)
        {
            var dir = Directory.GetFiles(workingDirectory).ToList();
            var file = string.Empty;
            foreach (var d in dir)
            {
                if (d.ToUpper().Contains("WATERMARKED"))
                {
                    file = d;
                    break;
                }
            }
            if (file.IsNullOrEmpty())
            {
                throw new InvalidDataException("Can not locate watermarked file!");
            }

            return file;
        }
    }
}
