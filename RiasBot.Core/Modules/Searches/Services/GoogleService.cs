using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.Translate.v2;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Translate.v2.Data;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Net;

namespace RiasBot.Modules.Searches.Services
{
    public class GoogleService : IRService
    {
        const string search_engine_id = "018084019232060951019:hs5piey28-e";
        private readonly IBotCredentials _creds;
        public GoogleService(IBotCredentials creds)
        {
            _creds = creds;
        }

        public async Task<string[]> YouTubeSearch(string type, string search)
        {
            if (String.IsNullOrEmpty(_creds.GoogleApiKey))
            {
                return new string[0];
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _creds.GoogleApiKey,
                ApplicationName = "Rias Bot"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = search; // Replace with your search term.
            searchListRequest.MaxResults = 15;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync().ConfigureAwait(false);

            int index = 0;
            int indexC = 0;
            string[] videos = new string[searchListResponse.Items.Count];
            string[] channels = new string[searchListResponse.Items.Count];

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                if (!String.IsNullOrEmpty(searchResult.Id.VideoId))
                {
                    if (type == "videolist")
                        videos[index] = searchResult.Snippet.Title + "&id=" + searchResult.Id.VideoId;
                    else
                        videos[index] = searchResult.Id.VideoId;
                    index++;
                }

                if (!String.IsNullOrEmpty(searchResult.Snippet.ChannelId))
                {
                    channels[indexC] = searchResult.Id.ChannelId;
                    indexC++;
                }
            }
            switch (type)
            {
                case "video":
                    return videos;
                case "channel":
                    return channels;
                case "videolist":
                    return videos;
            }
            return null;
        }

        public async Task<string[]> GoogleSearch(string keywords)
        {
            if (String.IsNullOrEmpty(_creds.GoogleApiKey))
            {
                return new string[0];
            }

            var googleService = new CustomsearchService(new BaseClientService.Initializer()
            {
                ApiKey = _creds.GoogleApiKey,
                ApplicationName = "Rias Bot"
            });
            var searchListRequest = googleService.Cse.List(keywords);
            searchListRequest.Cx = search_engine_id;
            var searchListResponse = await searchListRequest.ExecuteAsync().ConfigureAwait(false);

            if (searchListResponse.Items != null)
            {
                int index = 0;
                string[] results = new string[searchListResponse.Items.Count];

                foreach (var result in searchListResponse.Items)
                {
                    results[index] = result.Title + "&link=" + result.Link;
                    index++;
                }
                return results;
            }
            else
                return null;
        }

        public async Task<string[]> GoogleImageSearch(string keywords)
        {
            if (String.IsNullOrEmpty(_creds.GoogleApiKey))
            {
                return new string[0];
            }

            var googleService = new CustomsearchService(new BaseClientService.Initializer()
            {
                ApiKey = _creds.GoogleApiKey,
                ApplicationName = "Rias Bot"
            });
            var searchListRequest = googleService.Cse.List(keywords);
            searchListRequest.Cx = search_engine_id;
            searchListRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
            var searchListResponse = await searchListRequest.ExecuteAsync().ConfigureAwait(false);

            if (searchListResponse.Items != null)
            {
                int index = 0;
                string[] results = new string[searchListResponse.Items.Count];

                foreach (var result in searchListResponse.Items)
                {
                    results[index] = result.Link;
                    index++;
                }
                return results;
            }
            else
                return null;
        }
    }
}
