using System;
using EtsyServicer.DomainObjects;
using EtsyServices;
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


                _etsyService.Configure(new[] { "listings_w", "listings_r" });
                Listing listing = new Listing();

                var etsyListing = _etsyService.CreateListing(listing);


            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

    }
}
