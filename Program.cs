using System;
using System.Diagnostics;
using System.IO;
using AutoMapper;
using Inshapardaz.DataImport.Tasks;
using Microsoft.Extensions.Configuration;

namespace Inshapardaz.DataImport
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static void Main(string[] args)
        {
            Console.WriteLine($"Starting Process {Process.GetCurrentProcess().Id}");
            try
            {
                var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
                Configuration = builder.Build();

                Mapper.Initialize(c =>
                {
                    c.AddProfile(new MappingProfile());
                });
                Mapper.AssertConfigurationIsValid();

                //new OudDownloader().RemoveEmpty();
                //new OudParser(Configuration)
                    //.ParseAndSaveToJson();
                    //.ImportDataToDatabase();
                //new JsonDataImport(Configuration).ImportData(@"C:\Users\muhammad.farooq\Downloads\Words");

                //new UrduLughatParser(Configuration)
                    //.ParseDictioanryToJson()
                    //.ImportDataToDatabase();
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine("COMMAND FAILED");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
