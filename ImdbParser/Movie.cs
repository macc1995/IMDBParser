// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Movie.cs" company="KUKA Roboter GmbH">
//   Copyright (c) KUKA Roboter GmbH 2006 - 2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ImdbParser
{
   public class Movie
   {
      public string Title { get; set; }

      public string Description { get; set; }

      public string Category { get; set; }

      public string Release { get; set; }

      public string Rating { get; set; }

      public string ImageUrl { get; set; }

      public string Director { get; set; }

      public string Stars { get; set; }
   }
}