using BuildBoss;
using Newtonsoft.Json.Linq;
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
    internal sealed class ProjectUtil
    {
        private readonly XNamespace _namespace = SharedUtil.MSBuildNamespace;
        private readonly XmlNamespaceManager _manager;
        private readonly HashSet<string> _isFixedMap;
        private readonly string _filePath;
        private readonly XDocument _document;

        internal string ProjectName => Path.GetFileNameWithoutExtension(_filePath);

        internal bool IsPclProject
        {
            get
            {
                var elem = _document.XPathSelectElements("//mb:TargetFrameworkIdentifier", _manager).FirstOrDefault();
                if (elem == null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(elem.Value, ".NETPortable");
            }
        }

        internal bool IsDesktopProject
        {
            get
            {
                if (IsPclProject)
                {
                    return false;
                }

                var elem = _document.XPathSelectElements("//mb:TargetFrameworkVersion", _manager).FirstOrDefault();
                return elem != null;
            }
        }

        internal bool IsSharedProject => Path.GetExtension(_filePath) == ".shproj";

        internal bool IsExe
        {
            get
            {
                var elem = _document.XPathSelectElements("//mb:OutputType", _manager).FirstOrDefault();
                return elem != null && StringComparer.OrdinalIgnoreCase.Equals("Exe", elem.Value);
            }
        }

        internal ProjectUtil(string filePath, HashSet<string> isFixedMap)
        {
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            _isFixedMap = isFixedMap;
            _filePath = filePath;
            _document = XDocument.Load(_filePath);
        }

        internal void Convert()
        {
            if (IsPclProject)
            {
                ConvertPclProject();
            }
            else if (IsDesktopProject)
            {
                ConvertDesktopProject();
            }
            else if (IsSharedProject)
            {
                // Nothing to do for shared projects
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

            var propsElement = FindImportWithName("Settings.props");
            var targetElement = FindImportWithName("Imports.targets");

            var project = propsElement.Attribute("Project");
            project.Value = Path.Combine(Path.GetDirectoryName(project.Value), "SettingsSdk.props");

            var tfiElement = _document.XPathSelectElements("//mb:TargetFrameworkIdentifier", _manager).Single();
            var tfElement = new XElement(_namespace.GetName("TargetFramework"), GetTargetFrameworkValue());
            tfiElement.AddBeforeSelf(tfElement);
            tfiElement.Remove();
            _document.XPathSelectElements("//mb:TargetFrameworkVersion", _manager).Single().Remove();
            MaybeAddPackageTargetFallback(tfElement);

            // TODO: this affects project like ResultsProvider.  Check with tmat to make
            // sure this is okay.
            var tfpElement = _document.XPathSelectElements("//mb:TargetFrameworkProfile", _manager).FirstOrDefault();
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
            if (IsExe)
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
                if (cur.Name.ToLower().StartsWith("netcoreapp") && IsExe)
                {
                    prop = cur;
                    break;
                }

                if (cur.Name.ToLower().StartsWith("netstandard") && !IsExe)
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

        private XElement FindImportWithName(string fileName)
        {
            var all = _document.XPathSelectElements("//mb:Import", _manager);
            foreach (var e in all)
            {
                var project = e.Attribute("Project");
                if (project == null)
                {
                    continue;
                }

                var name = Path.GetFileName(project.Value);
                if (StringComparer.OrdinalIgnoreCase.Equals(name, fileName))
                {
                    return e;
                }
            }

            throw new Exception($"Unable to find Import for {fileName}");
        }

        internal void ConvertDesktopProject()
        {
            var itemGroup = GetPackageReferenceInsertElement();
            ConvertPackageReferences(inlineVersion: false);

            var tfvElement = _document.XPathSelectElements("//mb:TargetFrameworkVersion", _manager).Single();
            MaybeAddRuntimeIdentifiers(tfvElement);
            RemoveProjectJson();
        }

        // TODO: make sure to not disrupt package references that already exist
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

                var propName = GetPackageVersionPropertyName(package);
                var versionString = $"$({propName})";
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
            var groups = _document.XPathSelectElements("//mb:ItemGroup", _manager);
            var noneName = XName.Get("None", SharedUtil.MSBuildNamespaceUriRaw);
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
            var itemGroup = FindReferenceItemGroup();
            if (itemGroup != null)
            {
                return itemGroup;
            }

            var groups = _document.XPathSelectElements("//mb:ItemGroup", _manager);
            var last = groups.Last();
            var next = last.NextNode;
            itemGroup = new XElement(SharedUtil.MSBuildNamespace.GetName("ItemGroup"));
            next.AddBeforeSelf(itemGroup);
            return itemGroup;
        }

        private string GetPackageVersionPropertyName(NuGetPackage package)
        {
            var name = package.Name.Replace(".", "");
            var key = $"{name}FixedVersion:{package.Version}";
            if (_isFixedMap.Contains(key))
            {
                return $"{name}FixedVersion";
            }

            return $"{name}Version";
        }

        /// <summary>
        /// Find the last ItemGroup node that has Reference elements in it.
        /// </summary>
        private XElement FindReferenceItemGroup()
        {
            XElement e = null;

            var groups = _document.XPathSelectElements("//mb:ItemGroup", _manager);
            foreach (var group in groups)
            {
                if (group.Elements().Where(x => x.Name.LocalName == "Reference").Any())
                {
                    e = group;
                }
            }

            return e;
        }
    }
}
