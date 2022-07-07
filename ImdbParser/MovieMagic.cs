// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MovieMagic.cs" company="KUKA Roboter GmbH">
//   Copyright (c) KUKA Roboter GmbH 2006 - 2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ImdbParser
{
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Net.Http;
   using System.Text;
   using System.Threading.Tasks;

   using HtmlAgilityPack;

   public class MovieMagic
   {

      private async Task<Movie> GetMovie(HttpClient client, string link)
      {
           
         var baseNode = await GetNode(link);

         if (baseNode is null)
         {
            return null;
         }

         var title = GetNodes(baseNode, "h1", "class", "TitleHeader__TitleText").First().InnerText.Replace("&#x27;", "'");
         var rating = GetNodes(baseNode, "span", "class", "AggregateRatingButton__RatingScore").First().InnerText;
         var description = GetNodes(baseNode, "div", "class", "ipc-html-content ipc-html-content--base").First().ChildNodes.First().InnerText
            .Replace("&#x27;", "'").Replace("&#39;", "'");
         var image = GetNodes(baseNode, "div", "class", "ipc-media ipc-media--poster").First().ChildNodes.First(x => x.Name == "img")
            .GetAttributeValue("src", "");

         var genre = GetGenres(baseNode);

         var release = GetNodes(GetNodes(baseNode, "li", "data-testid", "title-details-releasedate").First(), "a", "class",
            "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link").First().InnerText;

         var credits = GetNodes(baseNode, "li", "data-testid", "title-pc-principal-credit");
         var directorNode = credits.First(x => x.ChildNodes.First().InnerText.ToLowerInvariant().Contains("director"));
         var director = GetNodes(directorNode, "a", "class",
            "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link").First().InnerText.Replace("&#x27;", "'");
         var starsNode = credits.First(x => x.ChildNodes.First().InnerText.ToLowerInvariant().Contains("stars"));
         var stars = GetStars(starsNode).Replace("&#x27;", "'");

         Console.ForegroundColor = ConsoleColor.Green;
         Console.WriteLine("!!Adding " + title + " -----------------!!");
         Console.ForegroundColor = ConsoleColor.White;
         return new Movie()
         {
            Title = title,
            Rating = rating,
            Description = description,
            ImageUrl = image,
            Category = genre,
            Release = release,
            Director = director,
            Stars = stars
         };
      }


      private static string GetStars(HtmlNode starsNode)
      {
         var nodes = GetNodes(starsNode, "a", "class", "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link");
         var sb = new StringBuilder();
         foreach (HtmlNode htmlNode in nodes)
         {
            sb.Append(htmlNode.InnerText + ",");
         }

         return sb.ToString().TrimEnd(',');
      }

      private static string GetGenres(HtmlNode baseNode)
      {
         var nodes = GetNodes(baseNode, "div", "class", "ipc-chip-list GenresAndPlot__GenresChipList").First().ChildNodes;
         var sb = new StringBuilder();
         foreach (HtmlNode htmlNode in nodes)
         {
            sb.Append(htmlNode.ChildNodes.First().InnerHtml + "/");
         }

         return sb.ToString().TrimEnd('/');
      }

      private static IEnumerable<HtmlNode> GetNodes(HtmlNode node, string elementType, string filterAttribute, string filter)
      {
         return node.Descendants().Where(x => (x.Name == elementType) && x.HasAttributes && x.GetAttributeValue(filterAttribute, "").Contains(filter))
            .ToList();
      }
      private async Task<HtmlNode> GetNode(string link)
      {

         try
         {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
               
            string doc = await client.GetStringAsync($"https://www.imdb.com{link}/");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(doc);
            HtmlNode node = htmlDoc.DocumentNode;
            var nodes = node.Descendants().ToList();

            Console.WriteLine("Checking: " + GetNodes(node, "h1", "class", "TitleHeader__TitleText").First().InnerText );

            //Check Tv series
            var inlineElements = nodes.Where(x => (x.Name == "li") && x.GetAttributeValue("class", "").Contains("ipc-inline-list__item")).ToList();
            if (inlineElements.Any(x => x.InnerHtml.ToLowerInvariant().Contains("tv series")))
            {
               Console.WriteLine("Tv series, excluding");
               return null;
            }

            //Check Tv Episodes
            //if (inlineElements.Any(x => x.InnerHtml.ToLowerInvariant().Contains("episode")))
            // {
            //     Console.WriteLine("Tv series, excluding");
            //      return null;
            //  }

            //Check rating
            if (GetNodes(node, "span", "class", "AggregateRatingButton__RatingScore").ToList().Count == 0)
            {
               Console.WriteLine("no rating, excluding");
               return null;
            }

            //Check description
            if (GetNodes(node, "div", "class", "ipc-html-content ipc-html-content--base").ToList().Count == 0)
            {
               Console.WriteLine("no description, excluding");
               return null;
            }

            //Check image
            if (GetNodes(node, "div", "class", "ipc-media ipc-media--poster").ToList().Count == 0)
            {
               Console.WriteLine("no image, excluding");
               return null;
            }

            //Check release date
            if ((GetNodes(node, "li", "data-testid", "title-details-releasedate").ToList().Count == 0)
                || (GetNodes(GetNodes(node, "li", "data-testid", "title-details-releasedate").First(), "a", "class",
                       "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link").ToList().Count == 0))
            {
               Console.WriteLine("no release date, excluding");
               return null;
            }

            //Check genre
            var genreNode = GetNodes(node, "div", "class", "ipc-chip-list GenresAndPlot__GenresChipList").ToList();
            if (genreNode.Count == 0)
            {
               Console.WriteLine("no genre, excluding");
               return null;
            }

            if (genreNode.First().ChildNodes.Count == 0)
            {
               Console.WriteLine("no genre, excluding");
               return null;
            }

            //check credits
            var creditNode = GetNodes(node, "li", "data-testid", "title-pc-principal-credit").ToList();
            if (creditNode.Count == 0 || !creditNode.Any(x => x.ChildNodes.First().InnerText.ToLowerInvariant().Contains("director")))
            {
               Console.WriteLine("no credits, excluding");
               return null;
            }


            var directorNode = creditNode.First(x => x.ChildNodes.First().InnerText.ToLowerInvariant().Contains("director"));
            if (GetNodes(directorNode, "a", "class", "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link")
                   .ToList().Count == 0)
            {
               Console.WriteLine("no director, excluding");
               return null;
            }

            var starsNode = creditNode.First(x => x.ChildNodes.First().InnerText.ToLowerInvariant().Contains("stars"));
            if (GetNodes(starsNode, "a", "class", "ipc-metadata-list-item__list-content-item ipc-metadata-list-item__list-content-item--link")
                   .ToList().Count == 0)
            {
               Console.WriteLine("no stars, excluding");
               return null;
            }

            return node;
         }
         catch (Exception e)
         {
            Console.WriteLine(e.Message);
            return null;
         }
      }

      public async Task AddMovies(List<Movie> list)
      {
         var client = new HttpClient();
         string doc = client.GetStringAsync($"https://www.imdb.com/chart/top").GetAwaiter().GetResult();
         var htmlDoc = new HtmlDocument();
         htmlDoc.LoadHtml(doc);
         HtmlNode node = htmlDoc.DocumentNode;
         var nodes = node.Descendants().ToList();

         var listNodes = GetNodes(node, "tbody", "class", "lister-list").First();

         var movieLinkNodes = GetNodes(listNodes, "td", "class", "titleColumn").ToList();
         //Parallel.ForEach(movieLinkNodes, movieLinkNode =>
         //{
         //   var link = movieLinkNode.ChildNodes.First(x => x.Name == "a").GetAttributeValue("href", "");
         //   var movie = GetMovie(client, link.Substring(0, link.IndexOf('?') - 1));
         //   list.Add(movie);
         //});

         var tasks = new List<Task<Movie>>();
         foreach (HtmlNode movieLinkNode in movieLinkNodes)
         {
            var link = movieLinkNode.ChildNodes.First(x => x.Name == "a").GetAttributeValue("href", "");
            tasks.Add(Task.Run((() =>  GetMovie(client, link.Substring(0, link.IndexOf('?') - 1)))));
         }

         Movie[] results = await Task.WhenAll(tasks.ToArray());
         list.AddRange(results);

      }
   }
}