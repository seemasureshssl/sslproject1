﻿/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using pCloud.NET;
using IgorSoft.CloudFS.Gateways.pCloud.Auth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.pCloud
{
    [ExportAsAsyncCloudGateway("pCloud")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), pCloudGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), pCloudGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), pCloudGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), pCloudGateway.API)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class pCloudGateway : IAsyncCloudGateway, IPersistGatewaySettings
    {
        private const string SCHEMA = "pcloud";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.CopyDirectoryItem;

        private const string URL = "https://www.pcloud.com";

        private const string API = "pCloud.NET SDK";

        private class pCloudContext
        {
            public pCloudClient Client { get; }

            public pCloudContext(pCloudClient client)
            {
                Client = client;
            }
        }

        private readonly IDictionary<RootName, pCloudContext> contextCache = new Dictionary<RootName, pCloudContext>();

        private readonly Policy retryPolicy = Policy.Handle<pCloudException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        private string settingsPassPhrase;

        [ImportingConstructor]
        public pCloudGateway([Import(ExportContracts.SettingsPassPhrase)] string settingsPassPhrase)
        {
            this.settingsPassPhrase = settingsPassPhrase;
        }

        private async Task<pCloudContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(pCloudContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var client = await Authenticator.LoginAsync(root.UserName, apiKey, settingsPassPhrase);
                contextCache.Add(root, result = new pCloudContext(client));
            }
            return result;
        }

        private static long ToId(DirectoryId folderId)
        {
            if (!folderId.Value.StartsWith("d", StringComparison.Ordinal))
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.InvalidFolderId, folderId));

            return long.Parse(folderId.Value.Substring(1), NumberStyles.Number);
        }

        private static long ToId(FileId fileId)
        {
            if (!fileId.Value.StartsWith("f", StringComparison.Ordinal))
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, Properties.Resources.InvalidFileId, fileId));

            return long.Parse(fileId.Value.Substring(1), NumberStyles.Number);
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try {
                await RequireContextAsync(root, apiKey);
                return true;
            } catch (AuthenticationException) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await retryPolicy.ExecuteAsync(() => context.Client.GetUserInfoAsync());

            return new DriveInfoContract(item.UserId, item.Quota - item.UsedQuota, item.UsedQuota);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await retryPolicy.ExecuteAsync(() => context.Client.ListFolderAsync(0));

            return new RootDirectoryInfoContract(item.Id, item.Created, item.Modified);
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var item = await retryPolicy.ExecuteAsync(() => context.Client.ListFolderAsync(ToId(parent)));
            var items = item.Contents;

            return items.Select(i => i.ToFileSystemInfoContract());
        }

        public async Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContextAsync(root);

            var locator = locatorResolver();
            await retryPolicy.ExecuteAsync(() => context.Client.UploadFileAsync(Stream.Null, ToId(locator.ParentId), locator.Name, CancellationToken.None));

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var stream = new ProducerConsumerStream();
            var retryPolicyWithAction = Policy.Handle<pCloudException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => stream.Reset());
            await retryPolicyWithAction.ExecuteAsync(() => context.Client.DownloadFileAsync(ToId(source), stream, CancellationToken.None));
            stream.Flush();

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContextAsync(root);

            var locator = locatorResolver();
            var stream = progress != null ? new ProgressStream(content, progress) : content;
            var retryPolicyWithAction = Policy.Handle<pCloudException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => content.Seek(0, SeekOrigin.Begin));
            await retryPolicyWithAction.ExecuteAsync(() => context.Client.UploadFileAsync(stream, ToId(locator.ParentId), locator.Name, CancellationToken.None));

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var fileSource = source as FileId;
            if (fileSource == null)
                 throw new NotSupportedException(Properties.Resources.CopyingOfDirectoriesNotSupported);

            var context = await RequireContextAsync(root);

            var item = await retryPolicy.ExecuteAsync(() => context.Client.CopyFileAsync(ToId(fileSource), ToId(destination), WebUtility.UrlEncode(copyName)));

            return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, (FileSize)item.Size, null);
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            var directorySource = source as DirectoryId;
            if (directorySource != null) {
                var item = await retryPolicy.ExecuteAsync(() => context.Client.RenameFolderAsync(ToId(directorySource), ToId(destination), WebUtility.UrlEncode(moveName)));

                return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
            }

            var fileSource = source as FileId;
            if (fileSource != null) {
                var item = await retryPolicy.ExecuteAsync(() => context.Client.RenameFileAsync(ToId(fileSource), ToId(destination), WebUtility.UrlEncode(moveName)));

                return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, (FileSize)item.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ItemTypeNotSupported, source.GetType().Name));
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var item = await retryPolicy.ExecuteAsync(() => context.Client.CreateFolderAsync(ToId(parent), WebUtility.UrlEncode(name)));

            return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var stream = progress != null ? new ProgressStream(content, progress) : content;
            var retryPolicyWithAction = Policy.Handle<pCloudException>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, ts) => content.Seek(0, SeekOrigin.Begin));
            var item = await retryPolicyWithAction.ExecuteAsync(() => context.Client.UploadFileAsync(stream, ToId(parent), WebUtility.UrlEncode(name), CancellationToken.None));

            return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, (FileSize)item.Size, null);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            var directoryTarget = target as DirectoryId;
            if (directoryTarget != null) {
                await retryPolicy.ExecuteAsync(() => context.Client.DeleteFolderAsync(ToId(directoryTarget), recurse));
                return true;
            }

            var fileTarget = target as FileId;
            if (fileTarget != null) {
                await retryPolicy.ExecuteAsync(() => context.Client.DeleteFileAsync(ToId(fileTarget)));
                return true;
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ItemTypeNotSupported, target.GetType().Name));
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContextAsync(root);

            var directoryTarget = target as DirectoryId;
            if (directoryTarget != null) {
                var locator = locatorResolver();
                var item = await retryPolicy.ExecuteAsync(() => context.Client.RenameFolderAsync(ToId(directoryTarget), ToId(locator.ParentId), newName));

                return new DirectoryInfoContract(item.Id, item.Name, item.Created, item.Modified);
            }

            var fileTarget = target as FileId;
            if (fileTarget != null) {
                var locator = locatorResolver();
                var item = await retryPolicy.ExecuteAsync(() => context.Client.RenameFileAsync(ToId(fileTarget), ToId(locator.ParentId), newName));

                return new FileInfoContract(item.Id, item.Name, item.Created, item.Modified, (FileSize)item.Size, null);
            }

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ItemTypeNotSupported, target.GetType().Name));
        }

        public void PurgeSettings(RootName root)
        {
            Authenticator.PurgeRefreshToken(root?.UserName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(pCloudGateway);
    }
}
