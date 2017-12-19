using ConvertPackageRef;
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
            Print(baseDir, "Roslyn.sln");
        }

        internal static void Print(string baseDir, string solutionRelativePath)
        {
            var solutionFullPath = Path.Combine(baseDir, solutionRelativePath);
            var solutionDir = Path.GetDirectoryName(solutionFullPath);
            foreach (var project in SolutionUtil.ParseProjects(solutionFullPath))
            {
                if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                {
                    continue;
                }

                var projectFilePath = project.GetFullPath(solutionDir);
                var util = new ProjectUtil(projectFilePath);
                Console.WriteLine($"{util.ProjectName}, {util.AssemblyName}");
            }
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

            var doc = util.MSBuildDocument;
            var changed = RemoveAll(doc, "ProjectGuid");
            changed |= CleanProjectReference(util);

            if (changed)
            {
                Console.WriteLine($"Processing {Path.GetFileName(projectFullPath)}");
                doc.Document.Save(projectFullPath);
            }
        }

        internal static bool RemoveAll(MSBuildDocument doc, params string[] toRemove)
        {
            var found = false;
            foreach (var item in toRemove)
            {
                var element = doc.XPathSelectElements(item).FirstOrDefault();
                if (element != null)
                {
                    element.Remove();
                    found = true;
                }
            }

            return found;
        }

        internal static bool CleanProjectReference(ProjectUtil util)
        {
            var changed = false;
            var projectDir = Path.GetDirectoryName(util.FilePath);
            var doc = util.MSBuildDocument;
            var elements = doc.XPathSelectElements("ProjectReference").ToList();
            foreach (var element in elements)
            {
                var refRelativePath = element.Attribute("Include").Value;
                var refFullPath = Path.Combine(projectDir, refRelativePath);
                var refUtil = new ProjectUtil(refFullPath);
                if (refUtil.IsNewSdk)
                {
                    var ns = util.MSBuildDocument.Namespace;
                    element.Element(ns.GetName("Project"))?.Remove();
                    element.Element(ns.GetName("Name"))?.Remove();
                    if (!element.Elements().Any())
                    {
                        var newElem = new XElement(ns.GetName("ProjectReference"));
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

            var doc = util.MSBuildDocument;

            var elements = doc.XPathSelectElements("Import");
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

            var projectElement = doc.XPathSelectElement("Project");
            var sdkAttr = new XAttribute(XName.Get("Sdk"), "Microsoft.NET.Sdk");
            projectElement.Attributes().Remove();
            projectElement.Add(sdkAttr);
            StripXmlNamespace(doc.Document);

            Console.WriteLine($"Processing {Path.GetFileName(projectFullPath)}");
            doc.Document.Save(projectFullPath);
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
    }
}
