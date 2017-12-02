﻿using ConvertPackageRef;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace QuickChange
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var baseDir = @"e:\code\roslyn";
            Convert(baseDir, "Roslyn.sln");
            Convert(baseDir, "Compilers.sln");
            Convert(baseDir, @"src\Samples\Samples.sln");
            Convert(baseDir, @"src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln");
            Convert(baseDir, @"src\Setup\Templates\Templates.sln");
        }

        internal static void Convert(string baseDir, string solutionRelativePath)
        {
            var solutionFullPath = Path.Combine(baseDir, solutionRelativePath);
            var solutionDir = Path.GetDirectoryName(solutionFullPath);
            foreach (var project in SolutionUtil.ParseProjects(solutionFullPath))
            {
                if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                {
                    continue;
                }

                var filePath = Path.Combine(Path.GetDirectoryName(solutionFullPath), project.RelativeFilePath);
                ConvertDirectoryProps(filePath);
            }
        }

        internal static void ConvertProject(string projectFullPath, string solutionDir)
        {
            var util = new ProjectUtil(projectFullPath);
            if (!util.IsNewSdk)
            {
                return;
            }

            var doc = util.Document;
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var changed = RemoveAll(doc, manager, "ProjectGuid");
            changed |= CleanProjectReference(util, manager);

            if (changed)
            {
                Console.WriteLine($"Processing {Path.GetFileName(projectFullPath)}");
                doc.Save(projectFullPath);
            }
        }

        internal static bool RemoveAll(XDocument doc, XmlNamespaceManager manager, params string[] toRemove)
        {
            var found = false;
            foreach (var item in toRemove)
            {
                var element = doc.XPathSelectElements($"//mb:{item}", manager).FirstOrDefault();
                if (element != null)
                {
                    element.Remove();
                    found = true;
                }
            }

            return found;
        }

        internal static bool CleanProjectReference(ProjectUtil util, XmlNamespaceManager manager)
        {
            var changed = false;
            var projectDir = Path.GetDirectoryName(util.FilePath);
            var doc = util.Document;
            var elements = doc.XPathSelectElements($"//mb:ProjectReference", manager).ToList();
            foreach (var element in elements)
            {
                var refRelativePath = element.Attribute("Include").Value;
                var refFullPath = Path.Combine(projectDir, refRelativePath);
                var refUtil = new ProjectUtil(refFullPath);
                if (refUtil.IsNewSdk)
                {
                    element.Element(SharedUtil.MSBuildNamespace.GetName("Project"))?.Remove();
                    element.Element(SharedUtil.MSBuildNamespace.GetName("Name"))?.Remove();
                    if (!element.Elements().Any())
                    {
                        var newElem = new XElement(SharedUtil.MSBuildNamespace.GetName("ProjectReference"));
                        newElem.Add(new XAttribute("Include", refRelativePath));
                        element.ReplaceWith(newElem);
                    }

                    changed = true;
                }
            }

            return changed;
        }

        internal static void ConvertDirectoryProps(string projectFullPath)
        {
            var util = new ProjectUtil(projectFullPath);
            if (!util.IsNewSdk)
            {
                return;
            }

            var doc = util.Document;
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);

            var elements = doc.XPathSelectElements($"//mb:Import", manager);
            foreach (var element in elements.ToList())
            {
                // VB uses <Import> for global namespaces
                var attribute = element.Attribute("Project");
                if (attribute == null)
                {
                    continue;
                }

                var project = attribute.Value;
                if (project.Contains("Settings.props") || project.Contains("SettingsSdk.props") || project.Contains("Imports.targets"))
                {
                    element.Remove();
                }
            }

            var projectElement = doc.XPathSelectElement("//mb:Project", manager);
            var sdkAttr = new XAttribute(XName.Get("Sdk"), "Microsoft.NET.Sdk");
            projectElement.Attributes().Remove();
            projectElement.Add(sdkAttr);
            StripXmlNamespace(doc);

            Console.WriteLine($"Processing {Path.GetFileName(projectFullPath)}");
            doc.Save(projectFullPath);
        }

        private static void StripXmlNamespace(XDocument document)
        {
            void Go(XElement element)
            {
                element.Name = element.Name.LocalName;
                foreach (var child in element.Elements())
                {
                    Go(child);
                }
            }

            foreach (var element in document.Elements())
            {
                Go(element);
            }
        }

        /// <summary>
        /// The Microsoft.NETCore.App reference will cause a warning during restore.
        /// </summary>
        internal static bool CleanMicrosoftNetCoreAppReferences(ProjectUtil util, XmlNamespaceManager manager)
        {
            if (util.IsMultiTargeted)
            {
                return false;
            }

            var targetPackage = util.IsExe
                ? "Microsoft.NETCore.App"
                : "NETStandard.Library";

            var changed = false;
            var doc = util.Document;
            var elements = doc.XPathSelectElements($"//mb:PackageReference", manager).ToList();
            XElement parent = null;
            foreach (var element in elements)
            {
                var item = element.Attribute("Include");
                if (StringComparer.OrdinalIgnoreCase.Equals(item.Value, targetPackage))
                {
                    parent = element.Parent;
                    element.Remove();
                    changed = true;
                }
            }

            if (parent != null && !parent.Elements().Any())
            {
                parent.Remove();
            }

            return changed;
        }
    }
}
