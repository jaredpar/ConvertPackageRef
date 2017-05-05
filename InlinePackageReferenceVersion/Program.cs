﻿using ConvertPackageRef;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InlinePackageReferenceVersion
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var map = ReadPackageVersionMap();
            foreach (var solution in args)
            {
                foreach (var project in SolutionUtil.ParseProjects(solution))
                {
                    if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                    {
                        continue;
                    }

                    var filePath = Path.Combine(Path.GetDirectoryName(solution), project.RelativeFilePath);
                    Convert(filePath, map);
                }
            }
        }

        internal static void Convert(string projectFilePath, Dictionary<string, string> packageMap)
        {
            var doc = XDocument.Load(projectFilePath);
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var tf = doc.XPathSelectElements("//mb:TargetFramework", manager).FirstOrDefault();
            if (tf != null)
            {
                return;
            }

            Console.WriteLine($"Processing {Path.GetFileName(projectFilePath)}");

            var versionName = SharedUtil.MSBuildNamespace.GetName("Version");
            foreach (var packageRef in doc.XPathSelectElements("//mb:PackageReference", manager))
            {
                var version = packageRef.Element(versionName);
                var value = version.Value.Trim();
                if (value.StartsWith("$("))
                {
                    value = value.Substring(2, value.Length - 3);
                    var rawVersion = packageMap[value];
                    version.Value = rawVersion;
                }
            }

            doc.Save(projectFilePath);
        }

        internal static Dictionary<string, string> ReadPackageVersionMap()
        {
            var packageVersionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadPackageVersionMapCore(packageVersionMap, @"e:\code\roslyn\build\Targets\Packages.props");
            ReadPackageVersionMapCore(packageVersionMap, @"e:\code\roslyn\build\Targets\FixedPackages.props");
            return packageVersionMap;
        }

        internal static Dictionary<string, string> ReadPackageVersionMapCore(Dictionary<string, string> packageVersionMap, string path)
        {
            var doc = XDocument.Load(path);
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var prop = doc.XPathSelectElements("//mb:PropertyGroup", manager).Single();
            foreach (var element in prop.Elements())
            {
                packageVersionMap.Add(element.Name.LocalName, element.Value.Trim());
            }

            return packageVersionMap;
        }

    }
}
