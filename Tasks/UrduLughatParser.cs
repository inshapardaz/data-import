using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using HtmlAgilityPack;
using Inshapardaz.DataImport.Database;
using Inshapardaz.DataImport.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Inshapardaz.DataImport.Tasks
{
    public class UrduLughatParser
    {
        private readonly IConfigurationRoot _configuration;

        List<string> ErrorParse = new  List<string>();
        List<string> Errors = new  List<string>();

        public UrduLughatParser(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public UrduLughatParser ParseDictioanryToJson()
        {
            var files = Directory.GetFiles(@"C:\code\urdu data\urdulughat.info\words\", "*.html").Where(f => !f.Contains("%")).ToArray();
            int i = 1;
            var dictionary = new Dictionary
            {
                Name = "urdulughat.info",
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


            var relations = new List<Relation>();
            foreach (var word in dictionary.Word)
            {
                foreach (var relation in word.WordRelationSourceWord)
                {
                    if (relation.RelatedWord != null)
                    {
                        relations.Add(new Relation{RelationType = relation.RelationType, SourceWord = word.Title, RelatedWord = relation.RelatedWord.Title } );
                    }
                }

                word.WordRelationSourceWord.Clear();
                word.WordRelationRelatedWord.Clear();
            }

            foreach (var word in dictionary.Word)
            {
                word.Id = 0;
            }

            System.IO.File.WriteAllText(@"C:\code\urdu data\urdulughat.info\errors.txt", string.Join(Environment.NewLine, Errors));
            System.IO.File.WriteAllText(@"C:\code\urdu data\urdulughat.info\errorParse.txt", string.Join(Environment.NewLine, ErrorParse));

            System.IO.File.WriteAllText(@"C:\code\urdu data\urdulughat.info\words.json", JsonConvert.SerializeObject(dictionary));
            System.IO.File.WriteAllText(@"C:\code\urdu data\urdulughat.info\relations.json", JsonConvert.SerializeObject(relations));

            return this;
        }

        public void ImportDataToDatabase()
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary>(System.IO.File.ReadAllText(@"C:\code\inshapardaz\data\urdulughat.info\words.json"));
            var relations = JsonConvert.DeserializeObject<List<Relation>>(System.IO.File.ReadAllText(@"C:\code\inshapardaz\data\urdulughat.info\relations.json"));

            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("InshapardazDatabase"));

            var database = new DatabaseContext(optionsBuilder.Options);
            database.Dictionary.Add(dictionary);
            database.SaveChanges();

            dictionary = database.Dictionary.Single(d => d.Name == dictionary.Name);
            foreach (var relation in relations)
            {
                var sourceWord = dictionary.Word.FirstOrDefault(w => w.DictionaryId == dictionary.Id && w.Title == relation.SourceWord);
                var relatedWord = dictionary.Word.FirstOrDefault(w => w.DictionaryId == dictionary.Id && w.Title == relation.RelatedWord);

                if (sourceWord != null && relatedWord != null)
                {
                    database.WordRelation.Add(new WordRelation {SourceWord = sourceWord, RelatedWord = relatedWord, RelationType = relation.RelationType});
                }
            }

            database.SaveChanges();
        }

        public void ParseFile(string filePath, Dictionary dictionary)
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

            var title = doc.DocumentNode.SelectSingleNode("//header/div/div/div/div/h2")?.InnerText.Trim();
            var alternameTitle = doc.DocumentNode.SelectSingleNode("//header/div/div/div/div[2]/span")?.InnerText;
            var pronounciation = doc.DocumentNode.SelectSingleNode("//header/div/div/div[2]")?.InnerText;
            var language = doc.DocumentNode.SelectSingleNode("//header/div/div/div[3]/div/span")?.InnerText;

            var detailsNode = doc.DocumentNode.SelectSingleNode("//div[@class='container details']");

            var description = detailsNode.SelectSingleNode("p")?.InnerText;

            var titleIndex = title.IndexOf('[');
            if (titleIndex != -1)
            {
                title = title.Substring(0, titleIndex);
            }

            titleIndex = alternameTitle.IndexOf('[');
            if (titleIndex != -1)
            {
                alternameTitle = alternameTitle.Substring(0, titleIndex);
            }
            var w = new Word
            {
                Id = WordIdFromfileName(Path.GetFileName(filePath)),
                Title = title,
                TitleWithMovements = alternameTitle.TrimBrackets(),
                Description = description.HtmlDecode(),
                Pronunciation = pronounciation.TrimBrackets(),
                Language = ParseLanguage(language)
            };

            if (dictionary.Word.Any(wd => wd.Id == w.Id))
            {
                throw new Exception();
            }

            var meaningNode = detailsNode.Descendants("div").SingleOrDefault(div => div.Id == "meanings");
            if (meaningNode != null)
            {

                var type = ParseGrammaticalTypes(meaningNode.SelectSingleNode("h5")?.InnerText);
                w.Attributes = type;

                var extraPropertiesNode = meaningNode.SelectSingleNode("div[@class='extra-properties']");
                if (extraPropertiesNode != null)
                {
                    var extraProperties = extraPropertiesNode.InnerText.Split(new []{']'}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var extraProperty in extraProperties)
                    {
                        ParseExtraProperty(extraProperty.Trim(), dictionary, w);
                    }
                }
                var meanings = meaningNode.SelectNodes("div[not(@class)]");
                if (meanings != null)
                {
                    foreach (var mean in meanings)
                    {
                        var val = mean.Descendants("div").FirstOrDefault()?.InnerText;
                        var context = string.Empty;
                        var contextStart = val.IndexOf('[');
                        if (contextStart != -1)
                        {
                            var contextEnd = val.IndexOf(']');
                            context = val.Substring(contextStart, contextEnd - contextStart)?.Trim('[',']').Trim();
                            if (!string.IsNullOrWhiteSpace(context))
                            {
                                val = val.Replace(context, string.Empty).RemoveBrackets();
                            } 
                        }
                        var example = mean.Descendants("div").ElementAtOrDefault(1)?.InnerText;

                        var numberIndex = val.IndexOf("-");
                        w.Meaning.Add(new Meaning
                        {
                            Value = val.Substring(numberIndex).Trim(),
                            Example = example.Trim(),
                            Context = context
                        });
                    }
                }
            }
            var synonymsNode = detailsNode.Descendants("div").SingleOrDefault(div => div.Id == "synonyms");
            ParseRelation(dictionary, w, synonymsNode, RelationType.Synonym);

            var compoundsNode = detailsNode.Descendants("div").SingleOrDefault(div => div.Id == "compounds");
            ParseRelation(dictionary, w, compoundsNode, RelationType.Compound);

            var usagesNode = detailsNode.Descendants("div").SingleOrDefault(div => div.Id == "usages");
            if (usagesNode != null)
            {
                var usageDivs = usagesNode.Descendants("div");
                foreach (var usageDiv in usageDivs)
                {
                    Word w2 = null;
                    foreach (var node in usageDiv.Descendants("div"))
                    {
                        if (node.Attributes["class"]?.Value == "meaning-title")
                        {
                            if (w2 != null)
                            {
                                dictionary.Word.Add(w2);
                                w.WordRelationRelatedWord.Add(new WordRelation
                                {
                                    RelationType = RelationType.Usage,
                                    RelatedWord = w2
                                });
                            }

                            var nodeInnerText = node.InnerText;
                            var index = nodeInnerText.IndexOf('-');
                            if (index != -1)
                                nodeInnerText = nodeInnerText.Substring(index + 1);
                            w2 = new Word{ Title = nodeInnerText.Trim().RemoveMovements() };
                        }
                        else
                        {
                            if (w2 != null)
                            {
                                var text = node.InnerText.Trim();
                                var index = text.IndexOf('۔');
                                if (index != -1)
                                {
                                    var mean = text.Substring(0, index);
                                    var example = text.Substring(index).Trim('۔');
                                    w2.Meaning.Add(new Meaning
                                    {
                                        Value = mean.Trim(),
                                        Example = example.Trim()
                                    });
                                }
                                else
                                {
                                    w2.Meaning.Add(new Meaning {Value = text.Trim()});
                                }
                            }
                        }
                    }

                    if (w2 != null)
                    {
                        dictionary.Word.Add(w2);
                        w.WordRelationSourceWord.Add(new WordRelation
                        {
                            RelationType = RelationType.Usage,
                            RelatedWord = w2
                        });
                    }
                }
            }
            var translationNode = detailsNode.Descendants("div").SingleOrDefault(div => div.Id == "english-translations");
            if (translationNode != null)
            {
                var ul = translationNode.SelectSingleNode("ul");
                var lis = ul?.Descendants("li");
                var translations = lis?.Select(n => n.InnerText);
                foreach (var translation in translations)
                {
                    foreach (var tran in translation.Split(';'))
                    {
                        w.Translation.Add(new Translation {Language = Languages.English, Value = tran.Trim()});
                    }
                }
            }

            dictionary.Word.Add(w);
        }

        private static void ParseExtraProperty(string extraProperties, Dictionary d, Word w)
        {
            if (string.IsNullOrWhiteSpace(extraProperties)) return;
            var index1 = extraProperties.IndexOf(":");
            if (index1 != -1)
            {
                var extraPropType = extraProperties.Substring(0, index1);
                var detail = extraProperties.Substring(index1).Trim(':');
                var index2 = detail.IndexOf("[");
                if (index2 != -1)
                {
                    var extraTitle = detail.Substring(0, index2).Trim().RemoveMovements();
                    var extraPronounciation = detail.Substring(index2).TrimBrackets();

                    Word w1 = new Word
                    {
                        Title = extraTitle,
                        Pronunciation = extraPronounciation
                    };

                    WordRelation relation = new WordRelation
                    {
                        SourceWord = w1,
                        RelatedWord = new Word {Title = extraTitle},
                        RelationType = ParseRelationType(extraPropType)
                    };

                    d.Word.Add(w1);

                    w.WordRelationSourceWord.Add(relation);
                }
            }
        }

        private void ParseRelation(Dictionary d, Word word, HtmlNode synonymsNode, RelationType relationType)
        {
            if (synonymsNode == null) return;   
            var listNode = synonymsNode.Descendants("li");
            foreach (var li in listNode)
            {
                var node = li.SelectSingleNode("a")??li;
                var title = node.SelectSingleNode("h5")?.InnerText?.RemoveMovements();
                var titleWithMovement = node.SelectSingleNode("meta")?.Attributes["content"]?.Value;
                var language = ParseLanguage(node.SelectSingleNode("div")?.InnerText);
                var id = WordIdFromfileName(node.Attributes["href"]?.Value);

                if (id <= 0)
                {
                    d.Word.Add(new Word{ Title = title});
                }

                var relation = new WordRelation
                {
                    SourceWord = word,
                    RelatedWord = new Word
                    {
                        Id = id > 0 ? id : 0,
                        Title = title,
                        TitleWithMovements = titleWithMovement,
                        Language = language
                    },
                    RelationType = relationType
                };

                word.WordRelationSourceWord.Add(relation);
            }
        }

        private GrammaticalType ParseGrammaticalTypes(string value)
        {
            var val = value?.Trim();

            if (string.IsNullOrWhiteSpace(val))
            {
                return GrammaticalType.None;
            }

            var startOfSubTypes = val.IndexOf("(");
            var endOfSubTypes = val.IndexOf(")");

            if (startOfSubTypes < 0)
            {
                return ParseGrammaticalType(val);
            }

            var a = val.Substring(0, startOfSubTypes);
            var b = val.Substring(startOfSubTypes, endOfSubTypes - startOfSubTypes);
            var split = b.TrimBrackets().Split('-');

            var grammaticalType = ParseGrammaticalType(a);
            grammaticalType = grammaticalType | ParseGrammaticalType(split.First());
            if (split.Length > 1)
            {
                grammaticalType = grammaticalType | ParseGrammaticalType(split.ElementAt(1));
            }

            return grammaticalType;
        }

        private static GrammaticalType ParseGrammaticalType(string value)
        {
            var val = value.Trim().TrimBrackets().RemoveDoubleSpaces(); 
            switch (val)
            {
                case "مذکر":
                    return GrammaticalType.Male;
                case "مؤنث":
                    return GrammaticalType.Female;
                case "مذکر، مؤنث":
                    return GrammaticalType.Male | GrammaticalType.Female;
                case "واحد":
                    return GrammaticalType.Singular;
                case "جمع":
                    return GrammaticalType.Plural;
                case "واحد، جمع":
                    return GrammaticalType.Singular | GrammaticalType.Plural;

                case "اسم":
                    return GrammaticalType.Ism;
                case "اسم نکرہ":
                    return GrammaticalType.IsmNakra;
                case "اسم کیفیت":
                    return GrammaticalType.IsmKaifiat;
                case "اسم مجرد":
                    return GrammaticalType.IsmMujarrad;
                case "اسم معرفہ":
                    return GrammaticalType.IsmMuarfa;
                case "اسم ظرف زماں":
                case "اسم ظرف زمان":
                    return GrammaticalType.IsmZarfZaman;
                case "اسم حاصل مصدر":
                    return GrammaticalType.IsmHasilMasdar;
                case "اسم مادہ":
                    return GrammaticalType.IsmMaada;
                case "اسم ظرف مکان":
                case "اسم ظرف مکاں":
                    return GrammaticalType.IsmZarfMakan;
                case "اسم آلہ":
                    return GrammaticalType.IsmAla;
                case "اسم علم":
                    return GrammaticalType.IsmAlam;
                case "اسم جمع":
                    return GrammaticalType.IsmJama;
                case "اسم صوت":
                    return GrammaticalType.IsmSoot;
                case "اسم تصغیر":
                    return GrammaticalType.IsmTashgeer;
                case "اسم ظرف":
                    return GrammaticalType.IsmZarf;
                case "اسم اشارہ":
                    return GrammaticalType.IsmIshara;
                case "اسم عدد":
                    return GrammaticalType.IsmAddad;
                case "اسم مصغر":
                    return GrammaticalType.IsmMasghar;
                case "اسم معاوضہ":
                    return GrammaticalType.IsmMuawqza;
                case "اسم جامد":
                    return GrammaticalType.IsmJamid;

                case "فعل لازم":
                    return GrammaticalType.FealLazim;
                case "فعل متعدی":
                    return GrammaticalType.FealMutaddi;

                case "صفت ذاتی":
                    return GrammaticalType.SiftZati;
                case "صفت عددی":
                    return GrammaticalType.SiftAdadi;
                case "صفت نسبتی":
                    return GrammaticalType.SiftNisbati;
                case "صفت مقداری":
                    return GrammaticalType.SiftMiqdari;

                case "حرف":
                    return GrammaticalType.Harf;
                case "حرف ربط":
                    return GrammaticalType.HarfRabt;
                case "حرف نفی":
                    return GrammaticalType.HarfNafi;
                case "حرف استثنا":
                    return GrammaticalType.HarfIstasna;
                case "حرف فجائیہ":
                    return GrammaticalType.HarfFijaia;
                case "حرف جزا":
                    return GrammaticalType.HarfJaza;
                case "حرف عطف":
                    return GrammaticalType.HarfAtaf;
                case "حرف جار":
                    return GrammaticalType.HarfJaar;
                case "حرف تخصیص":
                    return GrammaticalType.HarfTakhsees;
                case "حرف شرط":
                    return GrammaticalType.HarfShart;
                case "حرف تشبیہ":
                    return GrammaticalType.HarfTashbih;
                case "حرف ندائیہ":
                    return GrammaticalType.HarfNidaaiya;
                case "حرف دعائیہ":
                    return GrammaticalType.HarfDuaiya;
                case "حرف استعجاب":
                    return GrammaticalType.HarfIstasjab;

                case "متعلق فعل":
                    return GrammaticalType.MutaliqFeal;
                case "ضمیر شخصی":
                    return GrammaticalType.ZameerShakhsi;
                case "حاضر":
                    return GrammaticalType.Hazir;
                default:
                    throw new ArgumentOutOfRangeException(val);
            }
        }

        private static Languages ParseLanguage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Languages.None;
            }

            var val = value.Trim().TrimBrackets();
            switch (val)
            {
                case "سنسکرت":
                    return Languages.Sansikrat;
                case "عربی":
                    return Languages.Arabic;
                case "ترکی":
                    return Languages.Turkish;
                case "فارسی":
                    return Languages.Persian;
                case "ہندی":
                    return Languages.Hindi;
                case "پراکرت":
                    return Languages.Prakrit;
                case "اطالوی":
                    return Languages.Italian;
                case "انگریزی":
                    return Languages.English;
                case "مقامی":
                    return Languages.Local;
                case "اردو":
                    return Languages.Urdu;
                case "عبرانی":
                    return Languages.Hebrew;
                case "لاطینی":
                    return Languages.Latin;
                case "فارسی، ہندی":
                    return Languages.Persian;
                case "سنسکرت، پراکرت":
                    return Languages.Sansikrat;
                case "سنسکرت، فارسی":
                    return Languages.Sansikrat;
                case "اوستائی":
                    return Languages.Avestan;
                case "سندھی":
                    return Languages.Sindhi;
                case "یونانی":
                    return Languages.Greek;
                case "پنجابی":
                    return Languages.Punjabi;
                case "پہلوی":
                    return Languages.Persian;
                default:
                    throw new ArgumentOutOfRangeException(val);
            }
        }

        private static RelationType ParseRelationType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return RelationType.None;
            }

            var val = value.Trim().TrimBrackets();

            switch (val)
            {
                case "واحد غیر ندائی":
                    return RelationType.WahidGhairNadai;
                case "جمع":
                    return RelationType.Plural;
                case "جمع غیر ندائی":
                    return RelationType.JamaGhairNadai;
                case "جمع استثنائی":
                    return RelationType.JamaIstasnai;
                case "جنسِ مخالف":
                    return RelationType.OppositeGender;
                case "واحد":
                    return RelationType.Singular;
                case "تقابلی حالت":
                    return RelationType.Takabuli;
                case "جمع ندائی":
                    return RelationType.JamaNadai;
                case "واحد ندائی":
                    return RelationType.WahidNadai;
                case "تفضیلی حالت":
                    return RelationType.HalatTafseeli;
                case "حالت":
                    return RelationType.Halat;
                case "مفعولی حالت":
                    return RelationType.HalatMafooli;
                case "اضافی حالت":
                    return RelationType.HalatIzafi;
                default:
                     throw new ArgumentOutOfRangeException(val);
            }
        }
        private int WordIdFromfileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return 0;
            var val = fileName.Split('-').First();
            if (int.TryParse(val, out var parsedVal))
            {
                return parsedVal;
            }

            return -1;
        }

        public class Relation
        {
            public RelationType RelationType { get; set; }

            public string SourceWord { get; set; }

            public string RelatedWord { get; set; }

        }
    }
}
