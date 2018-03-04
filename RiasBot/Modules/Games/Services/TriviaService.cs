using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Modules.Games.Services
{
    public class TriviaService : IKService
    {
        public TriviaService()
        {

        }

        public string[] TriviaCategories =
        {
            "General Knowledge",
            "Books",
            "Film",
            "Music",
            "Musicals & Theatres",
            "Television",
            "Video Games",
            "Board Games",
            "Science & Nature",
            "Computers",
            "Mathematics",
            "Mythology",
            "Sports",
            "Geography",
            "History",
            "Politics",
            "Art",
            "Celebrities",
            "Animals",
            "Vehicles",
            "Comics",
            "Gadgets",
            "Anime & Manga",
            "Cartoon & Animations"
        };
    }
}
