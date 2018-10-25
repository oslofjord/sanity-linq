// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.If not, see<https://www.gnu.org/licenses/>.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sanity.Linq.Mutations
{
    public class SanityMutationBuilder<TDoc>
    {
        public SanityMutationBuilder(SanityMutationBuilder innerBuilder)
        {
            InnerBuilder = innerBuilder;
        }

        public SanityMutationBuilder InnerBuilder { get; }

        public object GetLock() => InnerBuilder.GetLock();

        public void Clear()
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations = InnerBuilder.Mutations.Where(m => m.DocType != typeof(TDoc)).ToList();
            }
        }

        public virtual string Build()
        {
            return InnerBuilder.Build(Mutations, InnerBuilder.Client.SerializerSettings );
        }

        public IReadOnlyList<SanityMutation> Mutations
        {
            get
            {
                lock (GetLock())
                {
                    return InnerBuilder.Mutations.Where(m => m.DocType == typeof(TDoc)).ToList();
                }
            }
        }

        public SanityMutationBuilder<TDoc> Create(TDoc document)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityCreateMutation(document) { DocType = typeof(TDoc) });
                return this;
            }
        }

        public SanityMutationBuilder<TDoc> CreateOrReplace(TDoc document)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityCreateOrReplaceMutation(document) { DocType = typeof(TDoc) });
                return this;
            }
        }


        public SanityMutationBuilder<TDoc> CreateIfNotExists(TDoc document)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityCreateIfNotExistsMutation(document) { DocType = typeof(TDoc) });
                return this;
            }
        }

        public SanityMutationBuilder<TDoc> DeleteById(string id)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityDeleteByIdMutation(id) { DocType = typeof(TDoc) });
                return this;
            }
        }


        public SanityMutationBuilder<TDoc> DeleteByQuery(Expression<Func<TDoc, bool>> query)
        {
            var parser = new SanityExpressionParser(query, typeof(TDoc));
            var sanityQuery = parser.BuildQuery(false);
            DeleteByQuery(sanityQuery);
            return this;
        }

        internal SanityMutationBuilder<TDoc> DeleteByQuery(Expression query)
        {
            var parser = new SanityExpressionParser(query, typeof(TDoc));
            var sanityQuery = parser.BuildQuery(false);
            DeleteByQuery(sanityQuery);
            return this;
        }


        public SanityMutationBuilder<TDoc> DeleteByQuery(string query)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityDeleteByQueryMutation(query) { DocType = typeof(TDoc) });
                return this;
            }
        }

        public SanityMutationBuilder<TDoc> PatchById(SanityPatchById<TDoc> patchById)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityPatchMutation(patchById) { DocType = typeof(TDoc) });
                return this;
            }
        }


        //public SanityTransactionBuilder<TDoc> PatchById(string id, object patch)
        //{
        //    lock (GetLock())
        //    {
        //        var sPatch = JsonConvert.SerializeObject(patch);
        //        var oPatch = JsonConvert.DeserializeObject<SanityPatchById<TDoc>>(sPatch);
        //        oPatch.Id = id;
        //        InnerBuilder.Mutations.Add(new SanityPatchMutation(oPatch) { DocType = typeof(TDoc) });
        //        return this;
        //    }
        //}


        public SanityMutationBuilder<TDoc> PatchById(string id, Action<SanityPatch> patch)
        {
            lock (GetLock())
            {
                var oPatch = new SanityPatchById<TDoc>(id);
                patch.Invoke(oPatch);
                InnerBuilder.Mutations.Add(new SanityPatchMutation(oPatch) { DocType = typeof(TDoc) });
                return this;
            }
        }


        public SanityMutationBuilder<TDoc> PatchByQuery(SanityPatchByQuery<TDoc> patchByQuery)
        {
            lock (GetLock())
            {
                InnerBuilder.Mutations.Add(new SanityPatchMutation(patchByQuery) { DocType = typeof(TDoc) });
                return this;
            }
        }

        //public SanityTransactionBuilder<TDoc> PatchByQuery(Expression<Func<TDoc, bool>> query, object patch)
        //{
        //    lock (GetLock())
        //    {
        //        var sPatch = JsonConvert.SerializeObject(patch);
        //        var oPatch = JsonConvert.DeserializeObject<SanityPatchByQuery<TDoc>>(sPatch);
        //        var parser = new SanityExpressionParser(query, typeof(TDoc));
        //        var sQuery = parser.BuildQuery();
        //        oPatch.Query = sQuery;
        //        InnerBuilder.Mutations.Add(new SanityPatchMutation(oPatch) { DocType = typeof(TDoc) });
        //        return this;
        //    }
        //}

        public SanityMutationBuilder<TDoc> PatchByQuery(Expression<Func<TDoc, bool>> query, Action<SanityPatch> patch)
        {
            lock (GetLock())
            {
                var oPatch = new SanityPatchByQuery<TDoc>(query);
                patch.Invoke(oPatch);
                var parser = new SanityExpressionParser(query, typeof(TDoc));
                InnerBuilder.Mutations.Add(new SanityPatchMutation(oPatch) { DocType = typeof(TDoc) });
                return this;
            }
        }

        internal SanityMutationBuilder<TDoc> PatchByQuery(Expression query, Action<SanityPatch> patch)
        {
            lock (GetLock())
            {
                var oPatch = new SanityPatchByQuery<TDoc>(query);
                patch.Invoke(oPatch);
                InnerBuilder.Mutations.Add(new SanityPatchMutation(oPatch) { DocType = typeof(TDoc) });
                return this;
            }
        }

        /// <summary>
        /// Updates an existing document. Note that fields with null values will simply be ignored 
        /// and NOT clear/null existing database values.
        /// If a revision field is present, this will be used to enforce optimistic concurrency control
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public SanityMutationBuilder<TDoc> SetValues(TDoc document)
        {
            lock (GetLock())
            {               
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                var id = document.SanityId();
                if (string.IsNullOrEmpty(id)) throw new Exception("Id must be specified when updating document.");

                InnerBuilder.Mutations.Add(new SanityPatchMutation(new SanityPatchById<TDoc>(id) { Set = document, IfRevisionId = document.SanityRevision() }) { DocType = typeof(TDoc) });
                return this;                
            }
        }

        /// <summary>
        /// Deletes and recreates existing document with new values. Use Set function to override values.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public SanityMutationBuilder<TDoc> Update(TDoc document)
        {
            lock (GetLock())
            { 
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                var id = document.SanityId();
                if (string.IsNullOrEmpty(id)) throw new Exception("Id must be specified when updating document.");

                InnerBuilder.Mutations.Add(new SanityDeleteByIdMutation(id) { DocType = typeof(TDoc) });
                InnerBuilder.Mutations.Add(new SanityPatchMutation(new SanityPatchById<TDoc>(id) { Set = document, IfRevisionId = document.SanityRevision() }) { DocType = typeof(TDoc) });
                return this;
            }
        
        }
    }

    public class SanityMutationBuilder
    {
        private SanityMutationBuilder()
        {
        }

        public object _lock = new object();

        public SanityMutationBuilder(SanityClient client)
        {
            Client = client;
        }

        public SanityClient Client { get; }

        public List<SanityMutation> Mutations { get; set; } = new List<SanityMutation>();

        public void Clear()
        {
            Mutations.Clear();
        }

        public virtual SanityMutationBuilder<TDoc> For<TDoc>()
        {
            return new SanityMutationBuilder<TDoc>(this);
        }

        public object GetLock() => _lock;

        public SanityMutationBuilder Create(object document)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityCreateMutation(document));
                return this;
            }
        }

        public virtual string Build(JsonSerializerSettings serializerSettings)
        {
            return Build(Mutations, serializerSettings);
        }

        public virtual string Build(IEnumerable<SanityMutation> mutations, JsonSerializerSettings serializerSettings)
        {
            return JsonConvert.SerializeObject(new { Mutations = mutations }, Formatting.None, serializerSettings);
        }

        public SanityMutationBuilder CreateOrReplace(object document)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityCreateOrReplaceMutation(document));
                return this;
            }
        }


        public SanityMutationBuilder CreateIfNotExists(object document)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityCreateIfNotExistsMutation(document));
                return this;
            }
        }

        public SanityMutationBuilder DeleteById(string id)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityDeleteByIdMutation(id));
                return this;
            }
        }

        public SanityMutationBuilder DeleteByQuery(Expression<Func<object, bool>> query)
        {
            var parser = new SanityExpressionParser(query, typeof(object));
            var sanityQuery = parser.BuildQuery(false);
            DeleteByQuery(sanityQuery);
            return this;
        }

        public SanityMutationBuilder DeleteByQuery(string query)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityDeleteByQueryMutation(query));
                return this;
            }
        }

        public SanityMutationBuilder PatchByQuery(SanityPatchByQuery patch)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityPatchMutation(patch));
                return this;
            }
        }

        public SanityMutationBuilder PatchByQuery(Expression<Func<object, bool>> query, object patch)
        {
            lock (GetLock())
            {
                var sPatch = JsonConvert.SerializeObject(patch);
                var oPatch = JsonConvert.DeserializeObject<SanityPatchByQuery>(sPatch);
                var parser = new SanityExpressionParser(query, typeof(object));
                var sQuery = parser.BuildQuery(false);
                oPatch.Query = sQuery;
                Mutations.Add(new SanityPatchMutation(oPatch));
                return this;
            }
        }

        public SanityMutationBuilder PatchByQuery(Expression<Func<object, bool>> query, Action<SanityPatch> patch)
        {
            lock (GetLock())
            {
                var oPatch = new SanityPatchByQuery(query);
                patch.Invoke(oPatch);
                var parser = new SanityExpressionParser(query, typeof(object));
                var sQuery = parser.BuildQuery(false);
                oPatch.Query = sQuery;
                Mutations.Add(new SanityPatchMutation(oPatch));
                return this;
            }
        }

        public SanityMutationBuilder PatchById(SanityPatchById patch)
        {
            lock (_lock)
            {
                Mutations.Add(new SanityPatchMutation(patch));
                return this;
            }
        }

        public SanityMutationBuilder PatchById(string id, object patch)
        {
            lock (GetLock())
            {
                var sPatch = JsonConvert.SerializeObject(patch);
                var oPatch = JsonConvert.DeserializeObject<SanityPatchById>(sPatch);
                oPatch.Id = id;
                Mutations.Add(new SanityPatchMutation(oPatch));
                return this;
            }
        }

        public SanityMutationBuilder PatchById(string id, Action<SanityPatch> patch)
        {
            lock (GetLock())
            {
                var oPatch = new SanityPatchById(id);
                patch.Invoke(oPatch);
                Mutations.Add(new SanityPatchMutation(oPatch));
                return this;
            }
        }



        /// <summary>
        /// Updates an existing document. Note that fields with null values will simply be ignored 
        /// and NOT clear/null existing database values.
        /// If a revision field is present, this will be used to enforce optimistic concurrency control
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public SanityMutationBuilder SetValues(object document)
        {
            lock (_lock)
            {
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                var id = document.SanityId();
                if (string.IsNullOrEmpty(id)) throw new Exception("Id must be specified when updating document.");

                Mutations.Add(new SanityPatchMutation(new SanityPatchById(id) { Set = document, IfRevisionId = document.SanityRevision() }));
                return this;
            }
        }

        /// <summary>
        /// Updates document by first deleting and then recreating document with new values.
        /// Use "set" for partial update of non-null values.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public SanityMutationBuilder Update(object document)
        {
            lock (_lock)
            {
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                var id = document.SanityId();
                if (string.IsNullOrEmpty(id)) throw new Exception("Id must be specified when updating document.");

                Mutations.Add(new SanityDeleteByIdMutation(id));
                Mutations.Add(new SanityPatchMutation(new SanityPatchById(id) { Set = document, IfRevisionId = document.SanityRevision() }));
                return this;
            }
        }

        /// <summary>
        /// Updates an existing document. Note that fields with null values will simply be ignored 
        /// and NOT clear/null existing database values.
        /// If a revision field is present, this will be used to enforce optimistic concurrency control
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public SanityMutationBuilder SetValues(string id, object document)
        {
            lock (_lock)
            {
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }
                if (string.IsNullOrEmpty(id)) throw new Exception("Id must be specified when updating document.");

                Mutations.Add(new SanityPatchMutation(new SanityPatchById(id) { Set = document, IfRevisionId = document.SanityRevision() }));
                return this;
            }
        }

    }
}
