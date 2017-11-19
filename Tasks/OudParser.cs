using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using Inshapardaz.DataImport.Database;
using Inshapardaz.DataImport.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using File = System.IO.File;

namespace Inshapardaz.DataImport.Tasks
{
    public class OudParser
    {
        private readonly IConfigurationRoot _configuration;

        List<string> ErrorParse = new List<string>();
        List<string> Errors = new List<string>();

        public OudParser(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public OudParser ParseAndSaveToJson()
        {
            var sourcePath = @"C:\code\urdu data\udb.org";
            var files = Directory.GetFiles(sourcePath, "*.html");
            int i = 1;
            var dictionary = new Dictionary
            {
                Name = "udb.org",
                Language = Languages.Urdu,
                IsPublic = true
            };
            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine($"Processing {i} of {files.Length} - {Path.GetFileName(file)}");
                    ParseFile(file, dictionary);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (!Errors.Contains(ex.ParamName))
                    {
                        Errors.Add(ex.ParamName);
                    }
                }
                catch (Exception ex)
                {
                    ErrorParse.Add(file);
                }
                finally
                {
                    i++;
                }
            }


            var relations = new List<UrduLughatParser.Relation>();
            foreach (var word in dictionary.Word)
            {
                foreach (var relation in word.WordRelationSourceWord)
                {
                    if (relation.RelatedWord != null && relation.RelatedWord.Id != 0)
                    {
                        relations.Add(new UrduLughatParser.Relation {RelationType = relation.RelationType, SourceWord = word.Title, RelatedWord = relation.RelatedWord.Title});
                    }
                }

                word.WordRelationSourceWord.Clear();
                word.WordRelationRelatedWord.Clear();
            }

            foreach (var word in dictionary.Word)
            {
                word.Id = 0;
            }

            File.WriteAllText(@"C:\code\urdu data\udb.org\output\errors.txt", string.Join(Environment.NewLine, Errors));
            File.WriteAllText(@"C:\code\urdu data\udb.org\output\errorParse.txt", string.Join(Environment.NewLine, ErrorParse));

            File.WriteAllText(@"C:\code\urdu data\udb.org\output\words.json", JsonConvert.SerializeObject(dictionary));
            File.WriteAllText(@"C:\code\urdu data\udb.org\output\relations.json", JsonConvert.SerializeObject(relations));

            return this;
        }

        public void ImportDataToDatabase()
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary>(System.IO.File.ReadAllText(@"C:\code\urdu data\udb.org\output\words.json"));
            //var relations = JsonConvert.DeserializeObject<List<Relation>>(System.IO.File.ReadAllText(@"C:\code\urdu data\urdulughat.info\relations.json"));

            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("InshapardazDatabase"));

            var database = new DatabaseContext(optionsBuilder.Options);
            database.Dictionary.Add(dictionary);
            database.SaveChanges();

            //dictionary = database.Dictionary.Single(d => d.Name == dictionary.Name);
            //foreach (var relation in relations)
            //{
            //    var sourceWord = dictionary.Word.FirstOrDefault(w => w.DictionaryId == dictionary.Id && w.Title == relation.SourceWord);
            //    var relatedWord = dictionary.Word.FirstOrDefault(w => w.DictionaryId == dictionary.Id && w.Title == relation.RelatedWord);

            //    if (sourceWord != null && relatedWord != null)
            //    {
            //        database.WordRelation.Add(new WordRelation { SourceWord = sourceWord, RelatedWord = relatedWord, RelationType = relation.RelationType });
            //    }
            //}

            database.SaveChanges();
        }

        private void ParseFile(string filePath, Dictionary dictionary)
        {
            var doc = new HtmlDocument();

            try
            {
                doc.Load(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in  {filePath} , {ex.Message}");
            }

            var container = doc.DocumentNode.SelectSingleNode("//body/div[@class='container']");

            var titleNode = container.SelectSingleNode("div/h1");
            var pronouciationNode = container.SelectSingleNode("div/h1/small");
            var grammaerNode = container.SelectSingleNode("div/h1/small/span");

            var titleAlternate = GetTitle(titleNode)?.Replace("-", string.Empty);
            var title = titleAlternate.RemoveMovements();
            var word = new Word
            {
                Title = title,
                TitleWithMovements = titleAlternate,
                Description = filePath,
                Pronunciation = GetPronounciation(pronouciationNode),
                Attributes = GetGrammarTypes(grammaerNode.InnerText)
            };

            if (string.IsNullOrWhiteSpace(word.Title)) return;
            
            var meaningNode = container.SelectSingleNode("div[2]");

            Meaning mean = null;
            foreach (var node in meaningNode.ChildNodes.Where(n => n.Name == "div"))
            {
                if (node.Attributes["class"]?.Value == "col-lg-12 col-md-12 col-sm-12 col-xs-12 set-width")
                {
                    if (mean != null)
                    {
                        word.Meaning.Add(mean);
                    }

                    mean = new Meaning {Value = StripNumber(node.InnerText.Trim())};
                }
                else if (node.Attributes["class"]?.Value == "col-lg-12 col-md-12 col-sm-12 col-xs-12 text-justify set-width")
                {
                    if (mean != null)
                    {
                        mean.Example += Environment.NewLine + node.InnerText.Trim();
                    }
                }
                else if (node.Attributes["class"]?.Value == "col-lg-12 col-md-12 col-sm-12 col-xs-12 text-center")
                {
                    //Word misc informaion here
                    word.Description = node.InnerText.HtmlDecode().Trim();
                }
            }
            if (mean != null)
            {
                word.Meaning.Add(mean);
            }

            dictionary.Word.Add(word);
        }

        private string StripNumber(string input)
        {
            var index = input.IndexOf("۔");
            if (index != -1)
            {
                return input.Substring(index);
            }
            return input;
        }

        private string GetTitle(HtmlNode node)
        {
            var extra = node?.SelectSingleNode("small")?.InnerText;
            if (extra != null)
                return node?.InnerText.Replace(extra, string.Empty).Trim();
            return node.InnerText.Trim();
        }

        private string GetPronounciation(HtmlNode node)
        {
            var extra = node.SelectSingleNode("span")?.InnerText;
            if (extra != null)
                return node.InnerText.Replace(extra, string.Empty).HtmlDecode().Trim().TrimBrackets();
            return node.InnerText.HtmlDecode().Trim().TrimBrackets();
        }


        public GrammaticalType GetGrammarTypes(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return GrammaticalType.None;

            var val = value.HtmlDecode().Trim().TrimBrackets().Trim();

            if (string.IsNullOrWhiteSpace(val)) return GrammaticalType.None;

            var retval = GrammaticalType.None;
            string previous = string.Empty;
            foreach (var v in val.Split(' '))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(previous))
                        retval |= GetGrammarType($"{previous.Trim()} {v.Trim()}");
                    else
                        retval |= GetGrammarType(v.Trim());

                    previous = string.Empty;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    previous = v;
                }
            }

            return retval;
        }

        public GrammaticalType GetGrammarType(string value)
        {
            switch (value)
            {
                case "ج":
                    return GrammaticalType.Plural;
                case "جج":
                    return GrammaticalType.JamaUlJama;
                case "مج":
                    return GrammaticalType.Majhool;
                case "مع":
                    return GrammaticalType.Maroof;
                case "امذ":
                    return GrammaticalType.Male | GrammaticalType.Ism;
                case "امث":
                    return GrammaticalType.Female | GrammaticalType.Ism;
                case "صف":
                    return GrammaticalType.Sift;
                case "مذ":
                    return GrammaticalType.Male;
                case "مث":
                    return GrammaticalType.Female;
                case "م ف":
                    return GrammaticalType.MutaliqFeal;
                case "ف ل":
                    return GrammaticalType.FealLazim;
                case "ف م":
                    return GrammaticalType.FealMutaddi;
                case "ف مر":
                    return GrammaticalType.FealMurakkab;
                case "محاورہ":
                    return GrammaticalType.Proverb;
                case "حرف":
                    return GrammaticalType.Harf;
                case "کہاوت":
                case "مقولہ":
                    return GrammaticalType.Saying;
                case "دیگر":
                    return GrammaticalType.None;
                case "نیز":
                    return GrammaticalType.None;
                case "فجائیہ":
                    return GrammaticalType.HarfFijaia;
                case "سابقہ":
                    return GrammaticalType.PreFix;
                case "لاحقہ":
                    return GrammaticalType.PostFix;
                default:
                    throw new ArgumentOutOfRangeException(value);
            }
        }

        public Languages GetLanguage(string value)
        {
            var val = value.HtmlDecode().Trim().RemoveBrackets().Trim();
            switch (val)
            {
                case "ا":
                    return Languages.Urdu;
                case "انگ":
                    return Languages.English;
                case "اوستا":
                    return Languages.Avestan;
                case "بنگ":
                    return Languages.Bangali;
                case "پ":
                    return Languages.Prakrit;
                case "پا":
                    return Languages.Pali;
                case "پر":
                    return Languages.Portugeese;
                case "پن":
                    return Languages.Punjabi;
                case "تر":
                    return Languages.Turkish;
                case "س":
                    return Languages.Sansikrat;
                case "سر":
                    return Languages.Syriac;
                case "ع":
                    return Languages.Arabic;
                case "عبر":
                    return Languages.Hebrew;
                case "ف":
                    return Languages.Persian;
                case "فر":
                    return Languages.French;
                case "گج":
                    return Languages.Gujrati;
                case "لاط":
                    return Languages.Latin;
                case "مر":
                    return Languages.Marhati;
                case "ہ":
                    return Languages.Hindi;
                case "یو":
                    return Languages.Greek;
                default:
                    throw new ArgumentOutOfRangeException(val);
            }
        }
    }
}
