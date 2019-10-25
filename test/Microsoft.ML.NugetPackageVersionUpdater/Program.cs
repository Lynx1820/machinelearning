﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Microsoft.ML.NugetPackageVersionUpdater
{
    class Program
    {
        private const string getLatestVersionBatFileName = "get-latest-package-version.bat";
        private const string tempVersionsFile = "latest_versions.txt";
        private const string targetPropsFile = "..\\PackageDependency.props";
        private const string packageNamespace = "Microsoft.ML";

        public static void Main(string[] args)
        {
            string projFilePath = targetPropsFile;
            var packageVersions = GetLatestPackageVersions();
            UpdatePackageVersion(projFilePath, packageVersions);
        }

        private static IDictionary<string, string> GetLatestPackageVersions()
        {
            Dictionary<string, string> packageVersions = new Dictionary<string, string>();

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = getLatestVersionBatFileName;
            p.Start();
            p.WaitForExit();

            using (var file = new StreamReader(tempVersionsFile))
            {
                var output = file.ReadToEnd();
                var splits = output.Split("\r\n");
                foreach (var split in splits)
                {
                    if (split.Contains(packageNamespace))
                    {
                        var detailSplit = split.Split(" ");

                        //valida NuGet package version should be separate by space like below:
                        //[PackageName]space[PackageVersion]
                        //One Example: Microsoft.ML 1.4.0-preview3-28223-2
                        if (detailSplit.Length == 2)
                            packageVersions.Add(detailSplit[0], detailSplit[1]);
                        else
                            throw new InvalidDataException($"Package version format is invalid for: {split}.");
                    }
                }
            }

            return packageVersions;
        }

        private static void UpdatePackageVersion(string filePath, IDictionary<string, string> latestPackageVersions)
        {
            string packageReferencePath = "/Project/ItemGroup/PackageReference";

            var CsprojDoc = new XmlDocument();
            CsprojDoc.Load(filePath);

            var packageReferenceNodes = CsprojDoc.DocumentElement.SelectNodes(packageReferencePath);

            for (int i = 0; i < packageReferenceNodes.Count; i++)
            {
                var packageName = packageReferenceNodes.Item(i).Attributes.GetNamedItem("Include").InnerText;

                if (latestPackageVersions.ContainsKey(packageName))
                {
                    var latestVersion = latestPackageVersions[packageName];
                    packageReferenceNodes.Item(i).Attributes.GetNamedItem("Version").InnerText = latestVersion;
                }
                else
                    throw new InvalidDataException($"Can't find latest version of Package {packageName} from NuGet source, fail to update version.");
            }

            CsprojDoc.Save(filePath);
        }
    }
}
