// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides an implementation of a <see cref='System.ComponentModel.LicenseProvider'/>. The provider works in
    ///    a similar fashion to Microsoft .NET Framework standard licensing module.</para>
    /// </summary>
    public class LicFileLicenseProvider : LicenseProvider
    {
        /// <summary>
        /// <para>Determines if the key retrieved by the <see cref='System.ComponentModel.LicFileLicenseProvider.GetLicense'/> method is valid 
        ///    for the specified type.</para>
        /// </summary>
        protected virtual bool IsKeyValid(string key, Type type)
        {
            if (key != null)
            {
                return key.StartsWith(GetKey(type));
            }
            return false;
        }


        /// <summary>
        ///    Creates a key for the specified type.
        /// </summary>
        protected virtual string GetKey(Type type)
        {
            // This string should not be localized.
            //
            return string.Format(CultureInfo.InvariantCulture, "{0} is a licensed component.", type.FullName);
        }


        /// <summary>
        ///    <para>Gets a license for the instance of the component and determines if it is valid.</para>
        /// </summary>
        public override License GetLicense(LicenseContext context, Type type, object instance, bool allowExceptions)
        {
            LicFileLicense lic = null;

            Debug.Assert(context != null, "No context provided!");
            if (context != null)
            {
                if (context.UsageMode == LicenseUsageMode.Runtime)
                {
                    string key = context.GetSavedLicenseKey(type, null);
                    if (key != null && IsKeyValid(key, type))
                    {
                        lic = new LicFileLicense(this, key);
                    }
                }

                if (lic == null)
                {
                    string modulePath = null;

                    if (context != null)
                    {
                        ITypeResolutionService resolver = (ITypeResolutionService)context.GetService(typeof(ITypeResolutionService));
                        if (resolver != null)
                        {
                            modulePath = resolver.GetPathOfAssembly(type.Assembly.GetName());
                        }
                    }

                    if (modulePath == null)
                    {
                        modulePath = type.Module.FullyQualifiedName;
                    }

                    string moduleDir = Path.GetDirectoryName(modulePath);
                    string licenseFile = moduleDir + "\\" + type.FullName + ".lic";

                    Debug.WriteLine("Looking for license in: " + licenseFile);
                    if (File.Exists(licenseFile))
                    {
                        Stream licStream = new FileStream(licenseFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                        StreamReader sr = new StreamReader(licStream);
                        string s = sr.ReadLine();
                        sr.Close();
                        if (IsKeyValid(s, type))
                        {
                            lic = new LicFileLicense(this, GetKey(type));
                        }
                    }

                    if (lic != null)
                    {
                        context.SetSavedLicenseKey(type, lic.LicenseKey);
                    }
                }
            }
            return lic;
        }

        private class LicFileLicense : License
        {
            private LicFileLicenseProvider _owner;
            private string _key;

            public LicFileLicense(LicFileLicenseProvider owner, string key)
            {
                _owner = owner;
                _key = key;
            }
            public override string LicenseKey
            {
                get
                {
                    return _key;
                }
            }
            public override void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
