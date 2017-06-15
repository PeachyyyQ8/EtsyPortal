using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
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


                var workingDirectory = GetWorkingDirectory();

                
                
                if (args[0].IsNullOrEmpty() || args[1].IsNullOrEmpty() || args[2].IsNullOrEmpty())
                {
                    throw new InvalidDataException("You must enter command line arguments in the following order:  Title, Price, IsCustomizable, Standard Tags, Additional Tags");
                }
                decimal price;
                bool isCustomizable;
                
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
                listing.Images = new[] { new ListingImage
                {
                    ImageFile = GetWatermarkedImage(workingDirectory),
                    Overwrite = true,
                    IsWatermarked = true,
                    Rank = 1}
                };
                listing.Tags = ParseTags(args);

                var zipName = GetZipName(workingDirectory);
                ZipFile.CreateFromDirectory(workingDirectory, zipName, CompressionLevel.Fastest, true);
                listing.DigitalFilePath = zipName;


                var etsyListing = _etsyService.CreateListing(listing);

            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        private static string GetWorkingDirectory()
        {
            var workingDirectory = SettingsHelper.GetAppSetting("workingDirectory");

            if (workingDirectory.IsNullOrEmpty())
            {
                Console.Write("Enter working directory: ");
                workingDirectory = Console.ReadLine();
                SettingsHelper.SetAppSetting("workingDirectory", workingDirectory);
            }
            else
            {
                Console.Write(
                    $"Working directory is currently set to {workingDirectory}.  Do you want to change it (Y if yes, Enter if No)?");
                if (Console.ReadLine().Trim().ToUpper() == "Y")
                {
                    var newDir = string.Empty;
                    while (workingDirectory.IsNullOrEmpty())
                    {
                        Console.Write("Enter working directory: ");
                        newDir = Console.ReadLine();
                        //detect whether its a directory or file
                        if (newDir.IsNullOrEmpty())
                        {
                            continue;
                        }
                        if ((File.GetAttributes(newDir) & FileAttributes.Directory) != FileAttributes.Directory)
                        {
                            continue;
                        }
                        workingDirectory = newDir;
                    }
                }
            }
            return workingDirectory;
        }

        private static string GetZipName(string workingDirectory)
        {
            var dir = Directory.GetFiles(workingDirectory).ToList();
            if (dir.Count == 0)
            {
                throw new Exception("Unable to get file name.  No files in Directory");
            }
            return Path.GetFileName(dir.First());
        }

        private static string[] ParseTags(IList<string> args)
        {
            var tagArray = new string[13];

            if (!args[3].IsNullOrEmpty())
            {
                args[3] = args[3].ToUpper();
            }

            Tags tags;
            var tagArrayIndex = 0;
            if (Enum.TryParse(args[3], out tags))
            {
                var standardTags = tags.StringValue().Split(',');
                for (int i = 0; i < standardTags.Count(); i++)
                {
                    tagArray[tagArrayIndex] = standardTags[i];
                    tagArrayIndex++;
                }
            }
            if (args.Count == 5)
            {
                var additionalTags = args[4].ToString().Split();
                for (var i = 0; i < additionalTags.Count(); i++)
                {
                    tagArray[tagArrayIndex] = additionalTags[i];
                }
            }
            return tagArray;
        }

        private static Image GetWatermarkedImage(string workingDirectory)
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

            var image = Image.FromFile(file);

            File.Delete(file);

            return image;
        }
    }
}
