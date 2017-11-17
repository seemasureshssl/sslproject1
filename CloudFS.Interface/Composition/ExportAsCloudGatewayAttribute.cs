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
using System.Composition;
using System.Globalization;

namespace IgorSoft.CloudFS.Interface.Composition
{
    /// <summary>
    /// Exports an <see cref="ICloudGateway"/> for MEF-composition.
    /// </summary>
    /// <seealso cref="ExportAttribute" />
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [CLSCompliant(false)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class ExportAsCloudGatewayAttribute : ExportAttribute
    {
        /// <summary>
        /// Gets the gateway name.
        /// </summary>
        /// <value>The gateway name.</value>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportAsCloudGatewayAttribute"/> class.
        /// </summary>
        /// <param name="name">The gateway name.</param>
        /// <exception cref="ArgumentNullException">The name is <c>null</c>.</exception>
        public ExportAsCloudGatewayAttribute(string name) : base(typeof(ICloudGateway))
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay() => $"{nameof(ExportAsCloudGatewayAttribute)} '{Name}'".ToString(CultureInfo.CurrentCulture);
    }
}
