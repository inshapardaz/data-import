using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inshapardaz.DataImport.Database;
using Inshapardaz.DataImport.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using File = System.IO.File;
using RelationType = Inshapardaz.DataImport.Database.Entities.RelationType;
using Word = Inshapardaz.DataImport.Database.Entities.Word;
using WordRelation = Inshapardaz.DataImport.Database.Entities.WordRelation;

namespace Inshapardaz.DataImport.Tasks
{
    public class JsonDataImport
    {
        private readonly IConfigurationRoot _configuration;

        public JsonDataImport(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public void ImportData(string filePath)
        {
            List<string> filesProcessed = new List<string>();
            var name = DateTime.Now.ToString("yyyyMMddhhmmss");

            try
            {
                Console.WriteLine($"Starting importing data from file '{filePath}'");

                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("InshapardazDatabase"));

                var database = new DatabaseContext(optionsBuilder.Options);
                database.ChangeTracker.AutoDetectChangesEnabled = false;
                var dictionaryId = 3;
                var dictionary
                    //= database.Dictionary.Single(d => d.Id == dictionaryId);
                    = new Database.Entities.Dictionary
                    {
                        Name = name,
                        IsPublic = true,
                        Language = Database.Entities.Languages.Urdu,
                        UserId = Guid.Empty
                    };

                database.Dictionary.Add(dictionary);
                database.SaveChanges();

                dictionaryId = dictionary.Id;
                Console.WriteLine($"Dictionary {dictionary.Name} Created.");

                var files = Directory.GetFiles(filePath, "*.json");
                int j = 0;
                foreach (var file in files)
                {
                    Model.Word w = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.Word>(File.ReadAllText(file));
                    w.Title = w.Title.Trim();
                    w.Description = w.Description.Trim();

                    foreach (var detail in w.WordDetails)
                    {
                        if (dictionary.Word.Any(dw => dw.Title == w.Title && (long) dw.Attributes == (long) detail.Attributes))
                        {
                            continue;
                        }
                        detail.WordInstance = w;
                        var word = AutoMapper.Mapper.Instance.Map<Word>(detail);
                        dictionary.Word.Add(word);


                        /*if (w.WordRelations != null && w.WordRelations.Any())
                        {
                            foreach (var rel in w.WordRelations)
                            {
                                Console.WriteLine($"{word.Id} Related to {rel.RelatedWord.Title}");

                                var relatedWord = database.Word.FirstOrDefault(wd => wd.Title == rel.RelatedWord.Title && wd.DictionaryId == dictionaryId);

                                if (relatedWord == null)
                                {
                                    relatedWord = new Word
                                    {
                                        Title = rel.RelatedWord.Title
                                    };
                                    dictionary.Word.Add(relatedWord);
                                }

                                database.WordRelation.Add(new WordRelation
                                {
                                    SourceWord = word,
                                    RelatedWord = relatedWord,
                                    RelationType = (RelationType) rel.RelationType
                                });
                            }
                        }*/
                    }

                    if (j % 100 == 0)
                    {
                        database.SaveChanges();
                        database.Dispose();
                        GC.Collect();
                        database = new DatabaseContext(optionsBuilder.Options);
                        dictionary = database.Dictionary.Single(d => d.Id == dictionaryId);
                    }

                    filesProcessed.Add(file);

                    j++;
                    Console.WriteLine($"Processing {j} of {files.Length} words");

                }

                database.SaveChanges();
                Console.WriteLine($"Completed importing data from file '{filePath}'. Imported {dictionary.Word.Count} words");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed importing data from file '{filePath}'");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            File.WriteAllText($"C:\\{name}.log"  , string.Join("\n", filesProcessed.ToArray()));
            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }
    }
}