﻿using Newtonsoft.Json.Linq;
using RepoUtil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertPackageRef
{
    internal sealed class ProjectConverterUtil
    {
        private readonly ProjectUtil _projectUtil;
        private readonly MSBuildDocument _msbuildDocument;
        private readonly XNamespace _namespace;
        private readonly Dictionary<string, string> _packageMap;
        private readonly HashSet<string> _isFixedMap;
        private readonly string _filePath;
        private readonly XDocument _document;

        internal string ProjectName => _projectUtil.ProjectName;


        internal ProjectConverterUtil(string filePath, Dictionary<string, string> packageMap, HashSet<string> isFixedMap)
        {
            _projectUtil = new ProjectUtil(filePath);
            _msbuildDocument = _projectUtil.MSBuildDocument;
            _namespace = _msbuildDocument.Namespace;
            _packageMap = packageMap;
            _isFixedMap = isFixedMap;
            _filePath = filePath;
            _document = _projectUtil.Document;
        }

        internal void Convert()
        {
            if (_projectUtil.IsPclProject)
            {
                ConvertPclProject();
            }
            else if (_projectUtil.IsDesktopProject)
            {
                ConvertDesktopProject();
            }
            else if (_projectUtil.IsSharedProject || _projectUtil.IsNewSdk)
            {
                // No need to convert
                return;
            }
            else
            {
                Console.WriteLine($"Unknown project {Path.GetFileName(_filePath)}");
                return;
            }

            _document.Save(_filePath);
        }

        internal void ConvertPclProject()
        {
            var importName = _namespace.GetName("Import");

            var propsElement = _projectUtil.FindImportWithName("Settings.props");
            var targetElement = _projectUtil.FindImportWithName("Imports.targets");

            var project = propsElement.Attribute("Project");
            project.Value = Path.Combine(Path.GetDirectoryName(project.Value), "SettingsSdk.props");

            var tfiElement = _msbuildDocument.XPathSelectElements("TargetFrameworkIdentifier").Single();
            var tfElement = new XElement(_namespace.GetName("TargetFramework"), GetTargetFrameworkValue());
            tfiElement.AddBeforeSelf(tfElement);
            tfiElement.Remove();
            _msbuildDocument.XPathSelectElements("TargetFrameworkVersion").Single().Remove();
            MaybeAddPackageTargetFallback(tfElement);

            if (_projectUtil.IsExe)
            {
                MaybeAddRuntimeIdentifiers(tfElement);
                RemoveLegacyNugetProperties();
            }

            var tfpElement = _msbuildDocument.XPathSelectElements("TargetFrameworkProfile").FirstOrDefault();
            if (tfpElement != null)
            {
                tfpElement.Remove();
            }

            if (!SkipPackageReferenceMerge())
            {
                ConvertPackageReferences(inlineVersion: true);
            }

            RemoveProjectJson();

            // Remove ToolsVersion
        }

        /// <summary>
        /// Calculate the TFM needed for New SDK projects.  In most cases could read this from the "frameworks" section
        /// of the project.json file.  For now just special casing the odd scenarios
        /// </summary>
        internal string GetTargetFrameworkValue()
        {
            if (_projectUtil.IsExe)
            {
                return "netcoreapp1.1";
            }

            switch (Path.GetFileNameWithoutExtension(_filePath))
            {
                case "MSBuildTask":
                    return "netstandard1.5";
                case "TestUtilities.CoreClr":
                    return "netstandard1.6";
                default:
                    return "netstandard1.3";
            }
        }

        internal bool SkipPackageReferenceMerge()
        {
            var name = Path.GetFileNameWithoutExtension(_filePath);
            return name == "CodeAnalysis"
                || name == "CSharpCodeAnalysis"
                || name == "CscCore"
                || name == "DeployCoreClrTestRuntime";
        }

        internal bool AllowDirectPackageReferenceVersions()
        {
            var name = Path.GetFileNameWithoutExtension(_filePath);
            return name == "RoslynInsertionTool.CommandLine"
                || name == "RoslynInsertionTool";
        }

        /// <summary>
        /// Remove all of the MSBuild elements we kept around for our legacy NuGet restore scenarios
        /// </summary>
        private void RemoveLegacyNugetProperties()
        {
            var elem = _msbuildDocument.XPathSelectElement("RuntimeIndentifier");
            if (elem != null)
            {
                if (elem.PreviousNode is XComment)
                {
                    elem.PreviousNode.Remove();
                }

                elem.Remove();
            }

            elem = _document.XPathSelectElement("NuGetRuntimeIdentifier");
            elem?.Remove();
        }

        /// <summary>
        /// Convert the frameworks / imports section of the project.json to a PackageTargetFallback element
        /// </summary>
        private void MaybeAddPackageTargetFallback(XElement target)
        {
            var json = GetProjectJsonFilePath();
            if (!File.Exists(json))
            {
                return;
            }

            var obj = JObject.Parse(File.ReadAllText(json), new JsonLoadSettings() { CommentHandling = CommentHandling.Load });
            var frameworks = (JObject)obj["frameworks"];
            JProperty prop = null;
            foreach (var cur in frameworks.Properties())
            {
                if (cur.Name.ToLower().StartsWith("netcoreapp") && _projectUtil.IsExe)
                {
                    prop = cur;
                    break;
                }

                if (cur.Name.ToLower().StartsWith("netstandard") && !_projectUtil.IsExe)
                {
                    prop = cur;
                    break;
                }

                if (cur.Name.ToLower().Contains("profile7"))
                {
                    var fallback = new XElement(_namespace.GetName("PackageTargetFallback"), "portable-net45+win8");
                    target.AddAfterSelf(fallback);
                    return;
                }
            }

            if (prop == null)
            {
                return;
            }

            var propValue = prop.Value as JObject;
            if (propValue == null)
            {
                return;
            }

            var imports = propValue.Property("imports");
            if (imports == null)
            {
                return;
            }

            var importValue = imports.Value;
            string content = null;
            switch (importValue.Type)
            {
                case JTokenType.String:
                    content = importValue.ToString();
                    break;
                case JTokenType.Array:
                    foreach (var elem in ((JArray)importValue).Values())
                    {
                        content = content == null
                            ? elem.ToString()
                            : $"{content};{elem}";
                    }
                    break;
            }

            if (content != null)
            {
                var fallback = new XElement(_namespace.GetName("PackageTargetFallback"), content);
                target.AddAfterSelf(fallback);
            }
        }

        /// <summary>
        /// Convert the runtimes section to the RuntimeIdentifiers section
        /// </summary>
        private void MaybeAddRuntimeIdentifiers(XElement target)
        {
            var json = GetProjectJsonFilePath();
            if (!File.Exists(json))
            {
                return;
            }

            var obj = JObject.Parse(File.ReadAllText(json), new JsonLoadSettings() { CommentHandling = CommentHandling.Load });
            var runtimes = (JObject)obj["runtimes"];
            if (runtimes == null)
            {
                return;
            }

            string content = null;
            foreach (var cur in runtimes.Properties())
            {
                var item = cur.Name;
                content = (content == null) ? item : $"{content};{item}";
            }

            if (content != null)
            {
                var e = new XElement(_namespace.GetName("RuntimeIdentifiers"), content);
                target.AddAfterSelf(e);
            }
        }

        internal void ConvertDesktopProject()
        {
            var itemGroup = GetPackageReferenceInsertElement();
            ConvertPackageReferences(inlineVersion: false);

            var tfvElement = _msbuildDocument.XPathSelectElements("TargetFrameworkVersion").Single();
            MaybeAddRuntimeIdentifiers(tfvElement);
            RemoveProjectJson();
        }

        internal void ConvertPackageReferences(bool inlineVersion)
        {
            var itemGroup = GetPackageReferenceInsertElement();
            var packageRefName = _namespace.GetName("PackageReference");
            var includeName = XName.Get("Include");
            foreach (var package in GetProjectJsonDependencies())
            {
                // TODO: this should work now.
                // This isn't supported on new SDK at the moment because it doesn't have a 
                // netstandard entry.  Ignore it for now.
                if (package.Name == "Microsoft.NETCore.Portable.Compatibility")
                {
                    continue;
                }

                var versionString = GetPackageVersionValue(package);
                var elem = new XElement(packageRefName);
                elem.Add(new XAttribute(includeName, package.Name));

                if (inlineVersion && !package.ExcludeCompile)
                {
                    elem.Add(new XAttribute("Version", versionString));
                }
                else
                {
                    elem.Add(new XElement(_namespace.GetName("Version"), versionString));
                    if (package.ExcludeCompile)
                    {
                        elem.Add(new XElement(_namespace.GetName("ExcludeAssets"), "compile"));
                    }
                }

                itemGroup.Add(elem);
            }
        }

        private ImmutableArray<NuGetPackage> GetProjectJsonDependencies()
        {
            var json = GetProjectJsonFilePath();
            if (!File.Exists(json))
            {
                return ImmutableArray<NuGetPackage>.Empty;
            }

            return ProjectJsonUtil.GetDependencies(json);
        }

        private string GetProjectJsonFilePath()
        {
            var dir = Path.GetDirectoryName(_filePath);
            return Path.Combine(dir, "project.json");
        }

        private void RemoveProjectJson()
        {
            var json = GetProjectJsonFilePath();
            if (File.Exists(json))
            {
                File.Delete(json);
            }

            RemoveProjectJsonElement();
        }

        private void RemoveProjectJsonElement()
        {
            var groups = _msbuildDocument.XPathSelectElements("ItemGroup");
            var noneName = _namespace.GetName("None");
            foreach (var group in groups.ToList())
            {
                var noneList = group.Elements(noneName).ToList();
                foreach (var e in noneList)
                {
                    var include = e.Attribute("Include");
                    if (StringComparer.OrdinalIgnoreCase.Equals(include?.Value, "project.json"))
                    {
                        e.Remove();
                    }
                }

                // Delete containing ItemGroup in the case project.json was the only item.
                if (!group.Elements().Any())
                {
                    group.Remove();
                }
            }
        }

        private XElement GetPackageReferenceInsertElement()
        {
            var itemGroup = _projectUtil.FindReferenceItemGroup();
            if (itemGroup != null)
            {
                return itemGroup;
            }

            itemGroup = _projectUtil.FindPackageReferenceItemGroup();
            if (itemGroup != null)
            {
                return itemGroup;
            }

            var groups = _msbuildDocument.XPathSelectElements("ItemGroup");
            var last = groups.Last();
            var next = last.NextNode;
            itemGroup = new XElement(_namespace.GetName("ItemGroup"));
            next.AddBeforeSelf(itemGroup);
            return itemGroup;
        }

        private string GetPackageVersionValue(NuGetPackage package)
        {
            var name = package.Name.Replace(".", "");
            var fixedKey = $"{name}FixedVersion:{package.Version}";
            if (_isFixedMap.Contains(fixedKey))
            {
                return $"$({name}FixedVersion)";
            }

            var normalKey = $"{name}Version";
            if (_packageMap.ContainsKey(normalKey))
            {
                return $"$({normalKey})";
            }

            if (AllowDirectPackageReferenceVersions())
            {
                return package.Version;
            }

            throw new Exception($"Unable to find the package key for {package.Name}");
        }
    }
}
