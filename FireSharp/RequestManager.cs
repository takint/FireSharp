﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Interfaces;

namespace FireSharp
{
    internal class RequestManager : IRequestManager
    {
        private readonly HttpClient _client;
        private readonly IFirebaseConfig _config;

        internal RequestManager(IFirebaseConfig config)
        {
            _config = config;
            _client = GetClient();
        }

        private HttpClient GetClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri(_config.BasePath),
                Timeout = TimeSpan.FromMinutes(_config.TimeoutInMinute)
            };
        }

        public void Dispose()
        {
            using (_client)
            {
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string path)
        {
            return await ProcessRequestAsync(HttpMethod.Get, path, null);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string path, T data)
        {
            return await ProcessRequestAsync(HttpMethod.Put, path, data);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string path, T data)
        {
            return await ProcessRequestAsync(HttpMethod.Post, path, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string path)
        {
            return await ProcessRequestAsync(HttpMethod.Delete, path, null, HttpCompletionOption.ResponseHeadersRead);
        }

        public async Task<HttpResponseMessage> PatchAsync<T>(string path, T data)
        {
            return await ProcessRequestAsync(new HttpMethod("PATCH"), path, data);
        }


        public HttpResponseMessage Get(string path)
        {
            return ProcessRequest(HttpMethod.Get, path, null);
        }

        public HttpResponseMessage Put<T>(string path, T data)
        {
            return ProcessRequest(HttpMethod.Put, path, data);
        }

        public HttpResponseMessage Post<T>(string path, T data)
        {
            return ProcessRequest(HttpMethod.Post, path, data);
        }

        public HttpResponseMessage Delete(string path)
        {
            return ProcessRequest(HttpMethod.Delete, path, null);
        }

        public HttpResponseMessage Patch<T>(string path, T data)
        {
            return ProcessRequest(new HttpMethod("PATCH"), path, data);
        }

        public HttpResponseMessage Listen(string path)
        {
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }, true)
            {
                BaseAddress = new Uri(_config.BasePath),
                Timeout = TimeSpan.FromMinutes(_config.TimeoutInMinute)
            };
            var uri = PrepareUri(path);

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
            response.EnsureSuccessStatusCode();

            return response;

        }

        public async Task<HttpResponseMessage> ListenAsync(string path)
        {
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }, true)
            {
                BaseAddress = new Uri(_config.BasePath),
                Timeout = TimeSpan.FromMinutes(_config.TimeoutInMinute)
            };

            var uri = PrepareUri(path);

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return response;
        }

        private async Task<HttpResponseMessage> ProcessRequestAsync(HttpMethod method, string path, object payload, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            try
            {
                var request = PrepareRequest(method, path, payload);

                return await GetClient().SendAsync(request, httpCompletionOption).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new FirebaseException(
                    string.Format("An error occured while execute request. Path : {0} , Method : {1}", path, method), ex);
            }
        }

        private HttpResponseMessage ProcessRequest(HttpMethod method, string path, object payload, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            try
            {
                var request = PrepareRequest(method, path, payload);

                return _client.SendAsync(request, httpCompletionOption).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new FirebaseException(
                    string.Format("An error occured while execute request. Path : {0} , Method : {1}", path, method), ex);
            }
        }

        private HttpRequestMessage PrepareRequest(HttpMethod method, string path, object payload)
        {
            var uri = PrepareUri(path);

            var request = new HttpRequestMessage(method, uri);

            if (payload != null)
            {
                var json = _config.Serializer.Serialize(payload);
                request.Content = new StringContent(json);
            }

            return request;
        }

        private Uri PrepareUri(string path)
        {
            var authToken = !string.IsNullOrWhiteSpace(_config.AuthSecret)
                ? string.Format("{0}.json?auth={1}", path, _config.AuthSecret)
                : string.Format("{0}.json", path);

            var url = string.Format("{0}{1}", _config.BasePath, authToken);

            return new Uri(url);
        }
    }
}