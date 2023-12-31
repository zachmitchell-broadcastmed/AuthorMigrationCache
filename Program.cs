﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AuthorMigrationCache
{
    internal class Program
    {
        private static int _numEmptyAuthorDivs = 0;
        private static int _completedArticles = 0;
        private static int _totalArticles = 0;
        private static readonly List<string> _pubSiteNames = new List<string>()
        {
            "Contact Lens Spectrum",
            "Corneal Physician",
            "Eyecare Business",
            "Glaucoma Physician",
            "New Retinal Physician",
            "Ophthalmic Professional",
            "Ophthalmology Management",
            "Optometric Management",
            "Presbyopia Physician",
            "Retinal Physician"
        };
        private static readonly List<string> _pubSiteUrls = new List<string>()
        {
            "https://www.clspectrum.com/issues/",
            "https://www.cornealphysician.com/issues/",
            "https://www.eyecarebusiness.com/issues/",
            "https://www.glaucomaphysician.net/issues/",
            "https://www.newretinalphysician.com/issues/",
            "https://www.ophthalmicprofessional.com/issues/",
            "https://www.ophthalmologymanagement.com/issues/",
            "https://www.optometricmanagement.com/issues/",
            "https://www.presbyopiaphysician.com/issues/",
            "https://www.retinalphysician.com/issues/"
        };
        private static readonly string _ioFileBase = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\ArticleInfo_";

        static async Task Main()
        {
            byte userChoice = GetSiteToPull();

            string basePubUrl = _pubSiteUrls[userChoice];
            List<ArticleInfo> articleInfos = new List<ArticleInfo>();
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            string[] fileLines;
            string inputFileName = (_ioFileBase + _pubSiteNames[userChoice]).Replace(' ', '_') + ".csv";

            try
            {

                using (StreamReader sr = new StreamReader(File.OpenRead(inputFileName)))
                {
                    fileLines = sr.ReadToEnd().Split(new char[] { '\n' });
                }
            }
            catch
            {
                Console.WriteLine($"ERROR: Looking for file at {inputFileName}!  Make sure this file exists and re-run the application.");
                Console.ReadLine();
                return;
            }

            //Task.Run(() => Interface.Start(fileLines.Length));
            _totalArticles = fileLines.Length - 1;

            for (int i = 1; i < fileLines.Length; ++i)
            {
                string currentLine = fileLines[i];
                string articleUrl = "";
                string nodeName = "";

                if (string.IsNullOrEmpty(currentLine))
                {
                    //Interface.IncrementCompleted();
                    Console.WriteLine($"[{++_completedArticles}/{_totalArticles}]  Empty line");
                    continue;
                }
                if (!currentLine.Contains("/Issues/"))
                {
                    Console.WriteLine($"[{++_completedArticles}/{_totalArticles}]  Non-issue line");
                    continue;
                }

                try
                {
                    string[] slashSplit = currentLine.Split(new char[] { '/' });
                    string year = slashSplit[2];

                    string[] articleTitleNodeCombo = slashSplit[4].Split(new char[] { ',' });
                    string articleNameDirty = articleTitleNodeCombo[0];
                    nodeName = articleTitleNodeCombo[1];

                    StringBuilder articleNameCleaned = new StringBuilder();
                    foreach(char c in articleNameDirty)
                    {
                        if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-')
                            articleNameCleaned.Append(c);
                    }
                    Console.WriteLine($"[{++_completedArticles}/{_totalArticles}]  Attempting scrape for article: " + articleNameCleaned);

                    string month = slashSplit[3];
                    if (month.Contains("-"))
                        month = month.Split(new char[] { '-' })[0];
                    
                    articleUrl = (basePubUrl + $"{year}/{month}-{year}/{articleNameCleaned}").ToLower();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                    errorList.Add(new ErrorInfo()
                    {
                        Url = articleUrl,
                        Error = e.Message
                    });
                }

                ArticleInfo ai = await FetchDataFromArticle(articleUrl);
                if (ai != null)
                {
                    ai.NodeName = nodeName.TrimEnd(new char[] { '\r' });
                    articleInfos.Add(ai);
                }

                //Interface.IncrementCompleted();
            }

            using (StreamWriter sw = new StreamWriter(File.Create(_ioFileBase + _pubSiteNames[userChoice] + ".json")))
            {
                string jsonText = JsonConvert.SerializeObject(articleInfos, Formatting.Indented);
                sw.WriteLine(jsonText);
            }

            if (errorList.Count > 0)
            {
                using (StreamWriter sw = new StreamWriter(File.Create(_ioFileBase + _pubSiteNames[userChoice] + "ERRORLOG.txt")))
                {
                    foreach (var v in errorList)
                    {
                        sw.WriteLine($"Error:\n{v.Url}\n{v.Error}\n\n");
                    }
                } 
            }

            //Interface.Stop();
            //Console.Clear();
            Console.WriteLine($"\nFiles without an author div: {_numEmptyAuthorDivs}");
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static byte GetSiteToPull()
        {
            bool validChoice = false;
            byte userChoice;

            do
            {
                Console.WriteLine("\nEnter the number for the site you wish to pull articles from as shown below:");
                for(int i = 0; i < _pubSiteNames.Count; ++i)
                {
                    Console.WriteLine($"{i})  {_pubSiteNames[i]}");
                }

                Console.WriteLine();
                string input = Console.ReadLine();
                if (byte.TryParse(input, out userChoice) && userChoice >= 0 && userChoice <= _pubSiteNames.Count - 1)
                    validChoice = true;
            } while (!validChoice);

            return userChoice;
        }

        private static async Task<ArticleInfo> FetchDataFromArticle(string articleUrl)
        {
            var clientHandler = new HttpClientHandler
            {
                UseCookies = false
            };
            var client = new HttpClient(clientHandler);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(articleUrl)
            };
            using (var response = await client.SendAsync(request))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responesBody = await response.Content.ReadAsStringAsync();
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(responesBody);

                    string headline = "";
                    var nodeList = doc.DocumentNode.Descendants().
                        Where(node => node.HasClass("c-article__headline"));
                    if (nodeList.Count() > 0)
                        headline = nodeList.First().InnerText;

                    string subheadline = "";
                    nodeList = doc.DocumentNode.Descendants().
                        Where(node => node.HasClass("c-article__subheadline"));
                    if(nodeList.Count() > 0)
                        subheadline = nodeList.First().InnerText;

                    DateTime pubDate = DateTime.MinValue;
                    nodeList = doc.DocumentNode.Descendants().Where(node => node.HasClass("c-article__dateline"));
                    if (nodeList.Count() > 0)
                        pubDate = DateTime.Parse(nodeList.First().InnerText);

                    string authorInner = "";
                    nodeList = doc.DocumentNode.Descendants().Where(node => node.HasClass("c-article__author"));
                    if (nodeList.Count() > 0)
                        authorInner = nodeList.First().InnerHtml;
                    else
                        ++_numEmptyAuthorDivs;

                    string body = "";
                    nodeList = doc.DocumentNode.Descendants().
                        Where(node => node.HasClass("c-article__body"));
                    if(nodeList.Count() > 0)
                        body = nodeList.First().InnerHtml;

                    return new ArticleInfo()
                    {
                        Title = headline,
                        Heading = subheadline,
                        PublishDate = pubDate,
                        Body = body,
                        Url = articleUrl,
                        Author = authorInner
                    };
                }
                else
                {
                    Console.WriteLine($"Error with url {response.StatusCode}: {articleUrl}");
                    return null;
                }
            }
        }
    }

    public class ArticleInfo
    {
        public string NodeName = "";
        public string Title = "";
        public string Heading = "";
        public DateTime PublishDate = DateTime.MinValue;
        public string Body = "";
        public string Url = "";
        public string Author = "";
    }

    public class ErrorInfo
    {
        public string Url = "";
        public string Error = "";
    }
}
