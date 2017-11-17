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
using System.Linq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.GatewayTests.Config;

namespace IgorSoft.CloudFS.GatewayTests
{
    public partial class GenericAsyncGatewayTests
    {
        private class TestDirectoryFixture : IDisposable
        {
            private readonly IAsyncCloudGateway gateway;

            private readonly RootName root;

            private readonly DirectoryInfoContract directory;

            internal DirectoryId Id => directory.Id;

            private TestDirectoryFixture(IAsyncCloudGateway gateway, RootName root, string apiKey, IDictionary<string, string> parameters, string path)
            {
                this.gateway = gateway;
                this.root = root;

                var rootDirectory = gateway.GetRootAsync(root, apiKey, parameters).Result;

                var residualDirectory = gateway.GetChildItemAsync(root, rootDirectory.Id).Result.SingleOrDefault(f => f.Name == path) as DirectoryInfoContract;
                if (residualDirectory != null)
                    gateway.RemoveItemAsync(root, residualDirectory.Id, true).Wait();

                directory = gateway.NewDirectoryItemAsync(root, rootDirectory.Id, path).Result;
            }

            internal static TestDirectoryFixture CreateTestDirectory(IAsyncCloudGateway gateway, GatewayElement config, GatewayTestsFixture fixture)
            {
                return new TestDirectoryFixture(gateway, fixture.GetRootName(config), config.ApiKey, fixture.GetParameters(config), config.TestDirectory);
            }

            internal DirectoryInfoContract ToContract()
            {
                return directory;
            }

            void IDisposable.Dispose()
            {
                gateway.RemoveItemAsync(root, directory.Id, true).Wait();
            }
        }
    }
}
