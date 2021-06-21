// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the MIT Licence.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  MIT Licence for more details.

//  You should have received a copy of the MIT Licence
//  along with this program.


using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.DTOs;
using Sanity.Linq.Internal;
using Sanity.Linq.Mutations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sanity.Linq
{
    public class SanityClient
    {
        protected SanityOptions _options;
        private IHttpClientFactory _factory;
        private HttpClient _httpQueryClient;
        private HttpClient _httpClient;

        public JsonSerializerSettings SerializerSettings { get; }

        public SanityClient(SanityOptions options, JsonSerializerSettings serializerSettings = null, IHttpClientFactory clientFactory = null)
        {
            _options = options;
            _factory = clientFactory;
            SerializerSettings = serializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new SanityReferenceTypeConverter() }
            };
            Initialize();
        }

        public virtual void Initialize()
        {
            // Initialize serialization settings

            // Initialize query client
            _httpQueryClient = _factory?.CreateClient() ?? new HttpClient();
            _httpQueryClient.DefaultRequestHeaders.Accept.Clear();
            _httpQueryClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_options.UseCdn)
            {
                _httpQueryClient.BaseAddress = new Uri($"https://{WebUtility.UrlEncode(_options.ProjectId)}.apicdn.sanity.io/v1/");
            }
            else
            {
                _httpQueryClient.BaseAddress = new Uri($"https://{WebUtility.UrlEncode(_options.ProjectId)}.api.sanity.io/v1/");
            }
            if (!string.IsNullOrEmpty(_options.Token) && !_options.UseCdn)
            {
                _httpQueryClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
            }

            // Initialize client for non-query requests (i.e. requests than never use CDN)
            if (!_options.UseCdn)
            {
                _httpClient = _httpQueryClient;
            }
            else
            {
                _httpClient = _factory?.CreateClient() ?? new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.BaseAddress = new Uri($"https://{WebUtility.UrlEncode(_options.ProjectId)}.api.sanity.io/v1/");
                if (!string.IsNullOrEmpty(_options.Token))
                {
                    _httpQueryClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
                }
            }

        }

        public virtual async Task<SanityQueryResponse<TResult>> FetchAsync<TResult>(string query, object parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }
            var oQuery = new SanityQuery
            {
                Query = query,
                Params = parameters
            };
            HttpResponseMessage response = null;
            if (_options.UseCdn)
            {
                // CDN only supports GET requests
                var url = $"data/query/{WebUtility.UrlEncode(_options.Dataset)}?query={WebUtility.UrlEncode(query ?? "")}";
                if (parameters != null)
                {
                    //TODO: Add support for parameters
                }
                response = await _httpQueryClient.GetAsync(url, cancellationToken);
            }
            else
            {
                // Preferred method is POST
                var json = new StringContent(JsonConvert.SerializeObject(oQuery, Formatting.None, SerializerSettings), Encoding.UTF8, "application/json");
                response = await _httpQueryClient.PostAsync($"data/query/{WebUtility.UrlEncode(_options.Dataset)}", json, cancellationToken).ConfigureAwait(false);
            }

            return await HandleHttpResponseAsync<SanityQueryResponse<TResult>>(response).ConfigureAwait(false);
        }

        public virtual async Task<SanityDocumentsResponse<TDoc>> GetDocumentAsync<TDoc>(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be empty", nameof(id));
            }
            var response = await _httpQueryClient.GetAsync($"data/doc/{WebUtility.UrlEncode(_options.Dataset)}/{WebUtility.UrlEncode(id)}", cancellationToken).ConfigureAwait(false);
            return await HandleHttpResponseAsync<SanityDocumentsResponse<TDoc>>(response).ConfigureAwait(false);
        }

        public virtual async Task<SanityDocumentResponse<SanityImageAsset>> UploadImageAsync(FileInfo image, string label = null, CancellationToken cancellationToken = default)
        {
            var mimeType = MimeTypeMap.GetMimeType(image.Extension);
            using (var fs = image.OpenRead())
            {
                return await UploadImageAsync(fs, image.Name, mimeType, label, cancellationToken).ConfigureAwait(false);
            }            
        }

        public virtual async Task<SanityDocumentResponse<SanityFileAsset>> UploadFileAsync(FileInfo file, string label = null, CancellationToken cancellationToken = default)
        {
            var mimeType = MimeTypeMap.GetMimeType(file.Extension);
            using (var fs = file.OpenRead())
            {
                return await UploadFileAsync(fs, file.Name, mimeType, label, cancellationToken).ConfigureAwait(false);
            }
        }


        public virtual async Task<SanityDocumentResponse<SanityImageAsset>> UploadImageAsync(Stream stream, string fileName, string contentType = null, string label = null, CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(fileName))
            {
                query.Add($"filename={WebUtility.UrlEncode(fileName)}");
            }
            if (!string.IsNullOrEmpty(label))
            {
                query.Add($"label={WebUtility.UrlEncode(label)}");
            }
            var uri = $"assets/images/{WebUtility.UrlEncode(_options.Dataset)}{(query.Count > 0 ? "?" + query.Aggregate((c,n) => c + "&" + n) : "")}";
            var request = new HttpRequestMessage(HttpMethod.Post, uri);

            request.Content = new StreamContent(stream);
            if (!string.IsNullOrEmpty(contentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }           

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await HandleHttpResponseAsync<SanityDocumentResponse<SanityImageAsset>>(response).ConfigureAwait(false);
        }

        public virtual async Task<SanityDocumentResponse<SanityFileAsset>> UploadFileAsync(Stream stream, string fileName, string contentType = null, string label = null, CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(fileName))
            {
                query.Add($"filename={WebUtility.UrlEncode(fileName)}");
            }
            if (!string.IsNullOrEmpty(label))
            {
                query.Add($"label={WebUtility.UrlEncode(label)}");
            }
            var uri = $"assets/files/{WebUtility.UrlEncode(_options.Dataset)}{(query.Count > 0 ? "?" + query.Aggregate((c, n) => c + "&" + n) : "")}";
            var request = new HttpRequestMessage(HttpMethod.Post, uri);

            request.Content = new StreamContent(stream);
            if (!string.IsNullOrEmpty(contentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await HandleHttpResponseAsync<SanityDocumentResponse<SanityFileAsset>>(response).ConfigureAwait(false);
        }

        public virtual Task<SanityMutationResponse> CommitMutationsAsync(object mutations, bool returnIds = false, bool returnDocuments = true, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            return CommitMutationsInternalAsync<SanityMutationResponse>(mutations, returnIds, returnDocuments, visibility, cancellationToken);
        }

        public virtual Task<SanityMutationResponse<TDoc>> CommitMutationsAsync<TDoc>(object mutations, bool returnIds = false, bool returnDocuments = true, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            return CommitMutationsInternalAsync<SanityMutationResponse<TDoc>>(mutations, returnIds, returnDocuments, visibility, cancellationToken);
        }

        protected virtual async Task<TResult> CommitMutationsInternalAsync<TResult>(object mutations, bool returnIds = false, bool returnDocuments = false, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            if (mutations == null)
            {
                throw new ArgumentNullException(nameof(mutations));
            }

            var json = mutations is string ? mutations as string : 
                       mutations is SanityMutationBuilder ? ((SanityMutationBuilder)mutations).Build(SerializerSettings) : 
                       JsonConvert.SerializeObject(mutations, Formatting.None, SerializerSettings);

            var response = await _httpClient.PostAsync($"data/mutate/{WebUtility.UrlEncode(_options.Dataset)}?returnIds={returnIds.ToString().ToLower()}&returnDocuments={returnDocuments.ToString().ToLower()}&visibility={visibility.ToString().ToLower()}", new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            return await HandleHttpResponseAsync<TResult>(response).ConfigureAwait(false);
        }

        protected virtual async Task<TResponse> HandleHttpResponseAsync<TResponse>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    return JsonConvert.DeserializeObject<TResponse>(content, SerializerSettings);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to deserialize Sanity response: {content}", ex);
                }
            }
            else
            {
                throw new SanityHttpException($"Sanity request failed with HTTP status {response.StatusCode}: {response.ReasonPhrase ?? ""}") { Content = content, StatusCode = response.StatusCode };
            }
        }

    }
}
