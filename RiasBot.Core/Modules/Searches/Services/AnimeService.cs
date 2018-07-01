using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches.Services
{
    public class AnimeService : IRService
    {
        public AnimeService()
        {

        }

        public async Task<dynamic> AnimeSearch(string anime)
        {
            var client = new GraphQLClient("https://graphql.anilist.co");
            var query = @"
                    query ($anime: String) {
                      Media(search: $anime, type: ANIME) {
                        id
                        siteUrl
                        title {
                          romaji
                          english
                          native
                        }
                        format
                        episodes
                        status
                        startDate {
                          year
                          month
                          day
                        }
                        endDate {
                          year
                          month
                          day
                        }
                        averageScore
                        meanScore
                        popularity
                        duration
                        genres
                        isAdult
                        description
                        coverImage {
                          large
                        }
                      }
                    }
                    ";
            return (await client.Query(query, new { anime })).Get("Media");
        }
        public async Task<dynamic> AnimeListSearch(string anime)
        {
            var client = new GraphQLClient("https://graphql.anilist.co");
            var query = @"
                    query ($anime: String) {
                      Page {
                        media (search: $anime, type:ANIME)
                        {
                          id
                          title
                          {
                            romaji
                            english
                            native
                          }
                          siteUrl
                        }
                      }
                    }
                    ";
            return (await client.Query(query, new { anime })).Get("Page");
        }

        public async Task<dynamic> CharacterSearch(string character)
        {
            var client = new GraphQLClient("https://graphql.anilist.co");
            var query = @"
                    query ($character: String) {
                      Page(page: 0) {
                        characters(search: $character) {
                          id
                          siteUrl
                          name {
                            first
                            last
                            alternative
                          }
                          description
                          image {
                            large
                          }
                        }
                      }
                    }
                    ";
            return (await client.Query(query, new { character }).ConfigureAwait(false)).Get("Page");
        }

        public async Task<dynamic> CharacterSearch(int character)
        {
            var client = new GraphQLClient("https://graphql.anilist.co");
            var query = @"
                    query ($character: Int) {
                      Character(id: $character) {
                        id
                        siteUrl
                        name {
                          first
                          last
                          alternative
                        }
                        description
                        image {
                          large
                        }
                      }
                    }
                    ";
            return (await client.Query(query, new { character }).ConfigureAwait(false)).Get("Character");
        }
    }
}
