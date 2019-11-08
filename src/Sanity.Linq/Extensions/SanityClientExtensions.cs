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

using Sanity.Linq.CommonTypes;
using Sanity.Linq.DTOs;
using Sanity.Linq.Internal;
using Sanity.Linq.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sanity.Linq
{
    public static class SanityClientExtensions
    {
        private static HttpClient _httpClient = new HttpClient();

        public static async Task<SanityDocumentResponse<SanityImageAsset>> UploadImageAsync(this SanityClient client, Uri imageUrl, string label = null, CancellationToken cancellationToken = default)
        {
            if (imageUrl == null)
            {
                throw new ArgumentNullException(nameof(imageUrl));
            }
            //Default to JPG
            var mimeType = MimeTypeMap.GetMimeType(".jpg");
            var fileName = imageUrl.PathAndQuery.Split('?')[0].Split('#')[0].Split('/').Last();
            var extension = fileName.Split('.').Last();
            if (extension != fileName)
            {
                mimeType = MimeTypeMap.GetMimeType(extension);
            }
            using (var fs = await _httpClient.GetStreamAsync(imageUrl).ConfigureAwait(false))
            {
                var result = await client.UploadImageAsync(fs, fileName, mimeType, label ?? "Source:" + imageUrl.OriginalString, cancellationToken).ConfigureAwait(false);
                fs.Close();
                return result;
            }
        }

        public static async Task<SanityDocumentResponse<SanityFileAsset>> UploadFileAsync(this SanityClient client, Uri fileUrl, string label = null, CancellationToken cancellationToken = default)
        {
            if (fileUrl == null)
            {
                throw new ArgumentNullException(nameof(fileUrl));
            }

            var mimeType = "application/octet-stream";
            var fileName = fileUrl.PathAndQuery.Split('?')[0].Split('#')[0].Split('/').Last();
            var extension = fileName.Split('.').Last();
            if (extension != fileName)
            {
                mimeType = MimeTypeMap.GetMimeType(extension);
            }
            using (var fs = await _httpClient.GetStreamAsync(fileUrl).ConfigureAwait(false))
            {
                var result = await client.UploadFileAsync(fs, fileName, mimeType, label ?? "Source:" + fileUrl.OriginalString, cancellationToken).ConfigureAwait(false);
                fs.Close();
                return result;
            }
        }

        public static Task<SanityMutationResponse<TDoc>> CreateAsync<TDoc>(this SanityClient client, TDoc document, CancellationToken cancellationToken = default)
        {
            return client.BeginTransaction<TDoc>().Create(document).CommitAsync(false, true, SanityMutationVisibility.Sync, cancellationToken);
        }

        public static Task<SanityMutationResponse<TDoc>> SetAsync<TDoc>(this SanityClient client, TDoc document, CancellationToken cancellationToken = default)
        {
            return client.BeginTransaction<TDoc>().SetValues(document).CommitAsync(false, true, SanityMutationVisibility.Sync, cancellationToken);
        }

        public static Task<SanityMutationResponse> DeleteAsync(this SanityClient client, string id, CancellationToken cancellationToken = default)
        {
            return client.BeginTransaction().DeleteById(id).CommitAsync(false, false, SanityMutationVisibility.Sync, cancellationToken);
        }

        public static SanityMutationBuilder BeginTransaction(this SanityClient client)
        {
            return new SanityMutationBuilder(client);
        }

        public static SanityMutationBuilder<TDoc> BeginTransaction<TDoc>(this SanityClient client)
        {
            return new SanityMutationBuilder(client).For<TDoc>();
        }

        public static Task<SanityMutationResponse> CommitAsync(this SanityMutationBuilder transactionBuilder, bool returnIds = false, bool returnDocuments = true, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            var result = transactionBuilder.Client.CommitMutationsAsync(transactionBuilder.Build(transactionBuilder.Client.SerializerSettings), returnIds, returnDocuments, visibility, cancellationToken);
            transactionBuilder.Clear();
            return result;
        }

        public static Task<SanityMutationResponse<TDoc>> CommitAsync<TDoc>(this SanityMutationBuilder<TDoc> transactionBuilder, bool returnIds = false, bool returnDocuments = true, SanityMutationVisibility visibility = SanityMutationVisibility.Sync, CancellationToken cancellationToken = default)
        {
            var result = transactionBuilder.InnerBuilder.Client.CommitMutationsAsync<TDoc>(transactionBuilder.Build(), returnIds, returnDocuments, visibility, cancellationToken);
            transactionBuilder.Clear();
            return result;
        }

    }
}
