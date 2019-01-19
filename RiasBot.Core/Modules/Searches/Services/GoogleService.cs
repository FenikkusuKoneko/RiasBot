using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using RiasBot.Services;
using System;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches.Services
{
    public class GoogleService : IRService
    {
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

            var index = 0;
            var indexC = 0;
            var videos = new string[searchListResponse.Items.Count];
            var channels = new string[searchListResponse.Items.Count];

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
    }
}
