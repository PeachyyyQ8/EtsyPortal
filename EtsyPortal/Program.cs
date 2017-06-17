using System;
using System.Collections.Generic;
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

                _etsyService.Configure(new[] { "listings_w", "listings_r" });
                Listing listing = new Listing
                {
                    Title = args[0] + ListingResources.ResourceManager.GetString("StandardTitle")
                };
                listing.Description = listing.Title + "\r\n\r\n" + ListingResources.ResourceManager.GetString("StandardDescription");
                listing.State = ListingStatus.Draft;
                listing.Quantity = "999";
                listing.Price = decimal.TryParse(args[1], out decimal price) ? price : throw new InvalidDataException("Price must be a decimal value.");
                listing.IsSupply = true;
                listing.CategoryId = 69150433;
                listing.WhenMade = "2010_2017";
                listing.WhoMade = "i_did";
                listing.IsCustomizable = bool.TryParse(args[2], out bool isCustomizable) && isCustomizable;
                listing.IsDigital = true;
                listing.ShippingTemplateId = 30116314577;
                listing.Images = new[] { new ListingImage
                {
                    ImagePath = GetWatermarkedImagePath(workingDirectory),
                    Overwrite = true,
                    IsWatermarked = true,
                    Rank = 1}
                };
                listing.Tags = ParseTags(args);

                var zip = CreateZipFile(workingDirectory);
                listing.DigitalFiles = new[] { new DigitalFile()
                    {
                        Path = zip,
                        Name = Path.GetFileName(zip),
                        Rank = 1}
                };

                var etsyListing = _etsyService.CreateListing(listing);

            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        private static string CreateZipFile(string workingDirectory)
        {
            var dir = Directory.GetFiles(workingDirectory).ToList();
            if (dir.Count == 0)
            {
                throw new Exception("Unable to get file name.  No files in Directory");
            }

            var zipName = Path.GetFileNameWithoutExtension(dir.First()) + ".zip";
            var zipFilePath = workingDirectory + @"\" + zipName;

            var tempDirectory = workingDirectory + @"\temp\";
            Directory.CreateDirectory(tempDirectory);

            foreach (var v in dir)
            {
                if (v.ToUpper().Contains("WATERMARK"))
                {
                    continue;
                }

                File.Move(v, workingDirectory + @"\temp\" + Path.GetFileName(v));
            }

            ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);

            Directory.Delete(tempDirectory, true);

            return zipFilePath;
        }

        private static string GetWorkingDirectory()
        {
            var workingDirectory = SettingsHelper.GetAppSetting("workingDirectory");

            if (workingDirectory.IsNullOrEmpty())
            {
                Console.Write(@"Enter working directory: ");
                workingDirectory = Console.ReadLine();
                SettingsHelper.SetAppSetting("workingDirectory", workingDirectory);
            }
            else
            {
                Console.Write(
                    $@"Working directory is currently set to {workingDirectory}.  Do you want to change it (Y if yes, Enter if No)?");
                var answer = Console.ReadLine();
                if (answer == null || answer.Trim().ToUpper() != "Y") return workingDirectory;

                while (workingDirectory.IsNullOrEmpty())
                {
                    Console.Write(@"Enter working directory: ");
                    var newDir = Console.ReadLine();

                    if (newDir.IsNullOrEmpty())
                    {
                        continue;
                    }
                    if (newDir != null && (File.GetAttributes(newDir) & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        continue;
                    }
                    workingDirectory = newDir;
                }
            }
            return workingDirectory;
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
                foreach (var t in standardTags)
                {
                    tagArray[tagArrayIndex] = t;
                    tagArrayIndex++;
                }
            }
            if (args.Count != 5) return tagArray;
            var additionalTags = args[4].Split();
            foreach (var t in additionalTags)
            {
                tagArray[tagArrayIndex] = t;
                tagArrayIndex++;
            }
            return tagArray;
        }

        private static string GetWatermarkedImagePath(string workingDirectory)
        {
            var dir = Directory.GetFiles(workingDirectory).ToList();
            var file = string.Empty;
            foreach (var d in dir)
            {
                if (!d.ToUpper().Contains("WATERMARKED")) continue;
                file = d;
                break;
            }
            if (file.IsNullOrEmpty())
            {
                throw new InvalidDataException("Can not locate watermarked file!");
            }

            return file;
        }
    }
}
