﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public class GraphQLClient
    {
        private class GraphQLQuery
        {
            // public string OperationName { get; set; }
            public string query { get; set; }
            public object variables { get; set; }
        }
        public class GraphQLQueryResult
        {
            private string raw;
            private JObject data;
            private Exception Exception;
            public GraphQLQueryResult(string text, Exception ex = null)
            {
                Exception = ex;
                raw = text;
                data = text != null ? JObject.Parse(text) : null;
            }
            public Exception GetException()
            {
                return Exception;
            }
            public string GetRaw()
            {
                return raw;
            }
            public T Get<T>(string key)
            {
                if (data == null) return default(T);
                try
                {
                    return JsonConvert.DeserializeObject<T>(this.data["data"][key].ToString());
                }
                catch
                {
                    return default(T);
                }
            }
            public dynamic Get(string key)
            {
                if (data == null) return null;
                try
                {
                    return JsonConvert.DeserializeObject<dynamic>(this.data["data"][key].ToString());
                }
                catch
                {
                    return null;
                }
            }
            public dynamic GetData()
            {
                if (data == null) return null;
                try
                {
                    return JsonConvert.DeserializeObject<dynamic>(this.data["data"].ToString());
                }
                catch
                {
                    return null;
                }
            }
        }
        private string url;
        public GraphQLClient(string url)
        {
            this.url = url;
        }
        public async Task<dynamic> Query(string query, object variables)
        {
            var fullQuery = new GraphQLQuery()
            {
                query = query,
                variables = variables,
            };
            string jsonContent = JsonConvert.SerializeObject(fullQuery);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(jsonContent.Trim());

            request.ContentLength = byteArray.Length;
            request.ContentType = @"application/json";

            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            }
            long length = 0;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    length = response.ContentLength;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        var json = await reader.ReadToEndAsync();
                        return new GraphQLQueryResult(json);
                    }
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = await reader.ReadToEndAsync();
                    Console.WriteLine(errorText);
                    return new GraphQLQueryResult(null, ex);
                }
            }
        }
    }
}
