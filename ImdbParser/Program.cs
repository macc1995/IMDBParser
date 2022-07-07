using System.Collections.Generic;
using System.Linq;
using System.Text;

using MySql.Data.MySqlClient;

namespace ImdbParser
{
   using System.CodeDom;
   using System.IO;
   using System.Net.Http.Headers;

   class Program
    {
       private static void Main(string[] args)
       {
          var list = new List<Movie>();
          
          var magic = new MovieMagic();
          magic.AddMovies(list).GetAwaiter().GetResult();

          var sb = new StringBuilder();

          foreach (Movie movie in list.Where(x=>x != null))
          {
             sb.AppendLine(
                $"{movie.Title};{movie.Release};{movie.Rating};{movie.Director};{movie.Description};{movie.Category};{movie.Stars};{movie.ImageUrl}");
          }

          File.WriteAllText(@"C:\Bazsiboi\movie.txt", sb.ToString());



         

        }

    }


}
