using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AuthorMigrationCache
{

    /// <summary>
    /// **Ignore - trying to asynchronously pool httprequests and wait for responses to save time
    /// Not implemented
    /// </summary>
    public class ArticleDataFetcher
    {
        public ArticleDataFetcher() { }

        public async Task<ArticleInfo> FetchDataFromArticle(string articleUrl)
        {
            try
            {
                Console.WriteLine(articleUrl);
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

                        string headline = doc.DocumentNode.Descendants().
                            Where(node => node.HasClass("c-article__headline")).First().InnerText;

                        string subheadline = doc.DocumentNode.Descendants().
                            Where(node => node.HasClass("c-article__subheadline")).First().InnerText;

                        //string body = doc.DocumentNode.Descendants().
                        //    Where(node => node.HasClass("s-cms c-article__body")).ToString();

                        return new ArticleInfo()
                        {
                            //Title = headline,
                            //Heading = subheadline,
                            //Body = body
                        };
                    }
                    else
                    {
                        Console.WriteLine($"Error with url {response.StatusCode}: {articleUrl}");
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
