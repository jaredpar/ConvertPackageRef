using BuildBoss;
using RepoUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertPackageRef
{
    internal sealed class PackageReferenceConverter
    {
        private readonly XmlNamespaceManager _manager;
        private readonly HashSet<string> _isFixedMap;

        internal PackageReferenceConverter(HashSet<string> isFixedMap)
        {
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            _isFixedMap = isFixedMap;
        }

        internal void ConvertSolution(string solutionPath)
        {
            var dir = Path.GetDirectoryName(solutionPath);
            foreach (var project in SolutionUtil.ParseProjects(solutionPath))
            {
                var projectPath = Path.Combine(dir, project.RelativeFilePath);
                ConvertProject(projectPath);
            }
        }

        internal void ConvertProject(string projectPath)
        {
            var dir = Path.GetDirectoryName(projectPath);
            var json = Path.Combine(dir, "project.json");
            if (!File.Exists(json))
            {
                return;
            }

            var projectFile = XDocument.Load(projectPath);
            var itemGroup = GetInsertElement(projectFile);
            var packageRefName = SharedUtil.MSBuildNamespace.GetName("PackageReference");
            var includeName = XName.Get("Include");
            var versionName = SharedUtil.MSBuildNamespace.GetName("Version");
            foreach (var package in ProjectJsonUtil.GetDependencies(json))
            {
                var propName = GetPackageVersionPropertyName(package);
                var elem = new XElement(packageRefName);
                elem.Add(new XAttribute(includeName, package.Name));
                elem.Add(new XElement(versionName, $"$({propName})"));
                itemGroup.Add(elem);
            }

            RemoveProjectJson(projectFile);

            projectFile.Save(projectPath);
            File.Delete(json);
        }

        private void RemoveProjectJson(XDocument doc)
        {
            var groups = doc.XPathSelectElements("//mb:ItemGroup", _manager);
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

        private XElement GetInsertElement(XDocument doc)
        {
            var itemGroup = FindReferenceItemGroup(doc);
            if (itemGroup != null)
            {
                return itemGroup;
            }

            var groups = doc.XPathSelectElements("//mb:ItemGroup", _manager);
            var last = groups.Last();
            var next = last.NextNode;
            itemGroup = new XElement(SharedUtil.MSBuildNamespace.GetName("ItemGroup"));
            next.AddBeforeSelf(itemGroup);
            return itemGroup;
        }

        private string GetPackageVersionPropertyName(NuGetPackage package)
        {
            var name = package.Name.Replace(".", "");
            var key = $"{name}FixedVersion{package.Version}";
            if (_isFixedMap.Contains(key))
            {
                return $"{name}FixedVersion";
            }

            return $"{name}Version";
        }

        /// <summary>
        /// Find the last ItemGroup node that has Reference elements in it.
        /// </summary>
        private XElement FindReferenceItemGroup(XDocument doc)
        {
            XElement e = null;

            var groups = doc.XPathSelectElements("//mb:ItemGroup", _manager);
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
