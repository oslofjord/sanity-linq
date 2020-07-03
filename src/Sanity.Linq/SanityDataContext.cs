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


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.DTOs;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.Mutations;
using Sanity.Linq.BlockContent;
using System.Threading;

namespace Sanity.Linq
{
    /// <summary>
    /// Linq-to-Sanity Data Context.
    /// Handles intialization of SanityDbSets defined in inherited classes.
    /// </summary>
    public class SanityDataContext
    {

        private object _dsLock = new object();
        private ConcurrentDictionary<string, SanityDocumentSet> _documentSets = new ConcurrentDictionary<string, SanityDocumentSet>();

        internal bool IsShared { get; }

        public SanityClient Client { get; }

        public SanityMutationBuilder Mutations { get; }

        public JsonSerializerSettings SerializerSettings { get; }

        public SanityHtmlBuilder HtmlBuilder { get; set; }

        /// <summary>
        /// Create a new SanityDbContext using the specified options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="isShared">Indicates that the context can be used by multiple SanityDocumentSets</param>
        public SanityDataContext(SanityOptions options, JsonSerializerSettings serializerSettings = null, SanityHtmlBuilderOptions htmlBuilderOptions = null, IHttpClientFactory clientFactory = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            SerializerSettings = serializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new SanityReferenceTypeConverter() }
            };
            Client = new SanityClient(options, serializerSettings, clientFactory);
            Mutations = new SanityMutationBuilder(Client);
            HtmlBuilder = new SanityHtmlBuilder(options, null, SerializerSettings, htmlBuilderOptions);
        }

       
        /// <summary>
        /// Create a new SanityDbContext using the specified options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="isShared">Indicates that the context can be used by multiple SanityDocumentSets</param>
        internal SanityDataContext(SanityOptions options, bool isShared) : this(options)
        {
            IsShared = isShared;
        }
             

        /// <summary>
        /// Returns an IQueryable document set for specified type
        /// </summary>
        /// <typeparam name="TDoc"></typeparam>
        /// <returns></returns>
        public virtual SanityDocumentSet<TDoc> DocumentSet<TDoc>(int maxNestingLevel = 7)
        {
            var key = $"{typeof(TDoc)?.FullName ?? ""}_{maxNestingLevel}";
            lock (_dsLock)
            {
                if (!_documentSets.ContainsKey(key))
                {
                    _documentSets[key] = new SanityDocumentSet<TDoc>(this, maxNestingLevel);
                }
            }
            return _documentSets[key] as SanityDocumentSet<TDoc>;
        }

        public virtual SanityDocumentSet<SanityImageAsset> Images => DocumentSet<SanityImageAsset>(2);

        public virtual SanityDocumentSet<SanityFileAsset> Files => DocumentSet<SanityFileAsset>(2);

        public virtual SanityDocumentSet<SanityDocument> Documents => DocumentSet<SanityDocument>(2);

        public virtual void ClearChanges()
        {
            Mutations.Clear();
        }

        /// <summary>
        /// Sends all changes registered on Document sets to Sanity as a transactional set of mutations.
        /// </summary>
        /// <param name="returnIds"></param>
        /// <param name="returnDocuments"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public async Task<SanityMutationResponse> CommitAsync(bool returnIds = false, bool returnDocuments = false, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            var result = await Client.CommitMutationsAsync(Mutations.Build(Client.SerializerSettings), returnIds, returnDocuments, visibility, cancellationToken).ConfigureAwait(false);
            Mutations.Clear();
            return result;
        }

        /// <summary>
        /// Sends all changes registered on document sets of specified type to Sanity as a transactional set of mutations.
        /// </summary>
        /// <param name="returnIds"></param>
        /// <param name="returnDocuments"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public async Task<SanityMutationResponse<TDoc>> CommitAsync<TDoc>(bool returnIds = false, bool returnDocuments = false, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            var mutations = Mutations.For<TDoc>();
            if (mutations.Mutations.Count > 0)
            {
                var result = await Client.CommitMutationsAsync<TDoc>(mutations.Build(), returnIds, returnDocuments, visibility, cancellationToken).ConfigureAwait(false);
                mutations.Clear();
                return result;
            }
            throw new Exception($"No pending changes for document type {typeof(TDoc)}");
        }

    }
}
