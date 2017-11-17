using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml;
using HtmlAgilityPack;
using Inshapardaz.DataImport.Database;
using Inshapardaz.DataImport.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using File = System.IO.File;

namespace Inshapardaz.DataImport.Tasks
{
    public class OudParser
    {

        public void ParseAndSaveToJson()
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
                    //ParseFile(file, dictionary);
                    ParseFile(@"C:\code\urdu data\udb.org\56840.html", dictionary);
                    break;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    //if (!Errors.Contains(ex.ParamName))
                    //{
                    //    Errors.Add(ex.ParamName);
                    //}
                }
                catch (Exception ex)
                {
                    //ErrorParse.Add(file);
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
                        relations.Add(new UrduLughatParser.Relation { RelationType = relation.RelationType, SourceWord = word.Title, RelatedWord = relation.RelatedWord.Title });
                    }
                }

                word.WordRelationSourceWord.Clear();
                word.WordRelationRelatedWord.Clear();
            }

            foreach (var word in dictionary.Word)
            {
                word.Id = 0;
            }

            File.WriteAllText(@"C:\code\urdu data\udb.org\output\words.json", JsonConvert.SerializeObject(dictionary));
            File.WriteAllText(@"C:\code\urdu data\udb.org\output\relations.json", JsonConvert.SerializeObject(relations));
        }

        /*public void Execute()
        {
            var connectionString = "data source=.;initial catalog=Inshapardaz;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            optionsBuilder.UseSqlServer(connectionString);

            var db = new DatabaseContext(optionsBuilder.Options);
            var files = new DirectoryInfo(@"C:\data\").GetFiles("*.html");
            var dictionary = new Dictionary {Name = DateTime.Now.ToString("u"), IsPublic = true, Language = Languages.Urdu };
            db.Dictionary.Add(dictionary);
            int i = 0;
            foreach (var file in files)
            {
                ProcessFile(dictionary, file.FullName);

                if (i % 100 == 0)
                {
                    db.SaveChanges();
                }
            }

            Console.ReadKey();
        }*/

        private void ParseFile(string filePath, Dictionary dictionary)
        {
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            var doc = new HtmlDocument();

            try
            {
                doc.Load(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in  {filePath} , {ex.Message}");
            }

            Console.WriteLine($"Processing {filePath}");

            var container = doc.DocumentNode.SelectSingleNode("//body/div[@class='container']");

            var titleNode = container.SelectSingleNode("div/h1");

            var word = new Word
            {
                Title = GetNodeText(titleNode),
                Description = filePath
            };

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

                    mean = new Meaning{Value = StripNumber(node.InnerText.Trim())};
                }
                else if (node.Attributes["class"]?.Value == "col-lg-12 col-md-12 col-sm-12 col-xs-12 text-justify set-width")
                {
                    if (mean != null)
                    {
                        mean.Example += Environment.NewLine + node.InnerText.Trim();
                    }
                }
            }
            
            Console.WriteLine(word);
            //var pronounceNode = titleNode.SelectSingleNode("small");
            //word.Pronunciation = GetNodeText(pronounceNode);

            /*var attribNode = doc.SelectSingleNode("/div/div[1]/h1/small/span");
            var attibute = GetType(GetNodeText(attribNode));

            var meaningNodes = doc.SelectSingleNode("/div/div[2]");
            word.Attributes = attibute;
            word.Meaning = new List<Meaning>();
            Meaning meaning = null;
            foreach (XmlNode meaningNode in meaningNodes.ChildNodes)
            {
                if (meaningNode.Name == "div" && meaningNode.FirstChild.Name != "p" &&
                    meaningNode.FirstChild.FirstChild.HasChildNodes &&
                    meaningNode.FirstChild.FirstChild.Attributes["class"].Value == "gold_color")
                {
                    if (meaning != null)
                    {
                        word.Meaning.Add(meaning);
                    }
                    meaning = new Meaning
                    {
                        Value = meaningNode.FirstChild.FirstChild.InnerText
                    };
                }
                if (meaning != null)
                {
                    meaning.Example += meaningNode.InnerText + Environment.NewLine;
                }
            }

            if (word != null)
            {
                dictionary.Word.Add(word);
            }*/
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

        private string GetNodeText(HtmlNode node)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Text)
                {
                    return child.InnerText.Trim();
                }
            }

            return string.Empty;
        }

        public GrammaticalType GetType(string value)
        {
            var grammaticalType = GrammaticalType.None;

            if (value == "ج")
            {
                grammaticalType = grammaticalType | GrammaticalType.Plural;
            }
            else if (value == "جج")
            {
                grammaticalType = grammaticalType | GrammaticalType.JamaUlJama;
            }
            else if (value == "مع")
            {
                grammaticalType = grammaticalType | GrammaticalType.Majhool;
            }
            else if (value == "امذ")
            {
                grammaticalType = grammaticalType | GrammaticalType.Male | GrammaticalType.Ism;
            }
            else if (value == "امث")
            {
                grammaticalType = grammaticalType | GrammaticalType.Female | GrammaticalType.Ism;
            }
            else if (value == "صف")
            {
                grammaticalType = grammaticalType | GrammaticalType.Sift;
            }
            else if (value == "مذ")
            {
                grammaticalType = grammaticalType | GrammaticalType.Male;
            }
            else if (value == "مث")
            {
                grammaticalType = grammaticalType | GrammaticalType.Female;
            }
            else if (value == "م ف")
            {
                grammaticalType = grammaticalType | GrammaticalType.MutaliqFeal;
            }
            else if (value == "ف ل")
            {
                grammaticalType = grammaticalType | GrammaticalType.FealLazim;
            }
            else if (value == "ف م")
            {
                grammaticalType = grammaticalType | GrammaticalType.FealMutaddi;
            }
            else if (value == "ف مر")
            {
                grammaticalType = grammaticalType | GrammaticalType.FealMurakkab;
            }

            return grammaticalType;
        }

        public Languages GetLanguage(string value)
        {
            if (value == "ا")
            {
                return Languages.Urdu;
            }
            if (value == "انگ")
            {
                return Languages.English;
            }
            if (value == "اوستا")
            {
                return Languages.Avestan;
            }
            if (value == "بنگ")
            {
                return Languages.Bangali;
            }
            if (value == "پ")
            {
                return Languages.Prakrit;
            }
            if (value == "پا")
            {
                return Languages.Pali;
            }
            if (value == "پر")
            {
                return Languages.Portugeese;
            }
            if (value == "پن")
            {
                return Languages.Punjabi;
            }
            if (value == "تر")
            {
                return Languages.Turkish;
            }
            if (value == "س")
            {
                return Languages.Sansikrat;
            }
            if (value == "سر")
            {
                return Languages.Syriac;
            }
            if (value == "ع")
            {
                return Languages.Arabic;
            }
            if (value == "عبر")
            {
                return Languages.Hebrew;
            }
            if (value == "ف")
            {
                return Languages.Persian;
            }
            if (value == "فر")
            {
                return Languages.French;
            }
            if (value == "گج")
            {
                return Languages.Gujrati;
            }
            if (value == "لاط")
            {
                return Languages.Latin;
            }
            if (value == "مر")
            {
                return Languages.Marhati;
            }
            if (value == "ہ")
            {
                return Languages.Hindi;
            }
            if (value == "یو")
            {
                return Languages.Greek;
            }
            return Languages.Unknown;
        }
    }
}
