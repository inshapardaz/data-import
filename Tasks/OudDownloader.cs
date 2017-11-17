using System;
using System.IO;
using System.Net.Http;

namespace Inshapardaz.DataImport.Tasks
{
    public class OudDownloader
    {
        private const string outputDir = @"C:\udb";
        private const string outputDir2 = @"C:\udb2";

        public void Download()
        {
            var urlPattern = "http://udb.gov.pk/result_details.php?word={0}";

            for (int i = 206171; i < 270000; i++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        using (HttpResponseMessage response = client.GetAsync(string.Format(urlPattern, i)).Result)
                        {
                            using (HttpContent content = response.Content)
                            {
                                string result = content.ReadAsStringAsync().Result;
                                System.IO.File.WriteAllText(Path.Combine(outputDir, string.Format("{0:D7}.html", i)), result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error download html");
                }

                Console.WriteLine($"Processed page {i}");
            }
        }

        public void RemoveEmpty()
        {
            var files = Directory.GetFiles(outputDir, "*.html");
            foreach (var file in files)
            {
                Console.WriteLine($"Processing {file}");
                var contents = File.ReadAllText(file);
                if (contents.Contains("result_details.php?word="))
                {
                    File.Copy(file, file.Replace(outputDir, outputDir2));
                }
            }
        }
    }
}
