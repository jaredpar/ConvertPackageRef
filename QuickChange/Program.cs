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
            Convert(baseDir, "Roslyn.sln");
            Convert(baseDir, "Compilers.sln");
            Convert(baseDir, "CrossPlatform.sln");
            Convert(baseDir, @"src\Samples\Samples.sln");
        }

        internal static void Convert(string baseDir, string solutionRelativePath)
        {
            var solutionFullPath = Path.Combine(baseDir, solutionRelativePath);
            foreach (var project in SolutionUtil.ParseProjects(solutionFullPath))
            {
                if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                {
                    continue;
                }

                var filePath = Path.Combine(Path.GetDirectoryName(solutionFullPath), project.RelativeFilePath);
                ConvertProject(filePath);
            }
        }

        internal static void ConvertProject(string projectFullPath)
        {
            var util = new ProjectUtil(projectFullPath);
            if (!util.IsNewSdk)
            {
                return;
            }


            var doc = util.Document;
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);

            var toRemove = new[]
            {
                "ProjectTypeGuids",
                "TargetFrameworkProfile"
            };

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

            if (found)
            {
                Console.WriteLine($"Processing {Path.GetFileName(projectFullPath)}");
                doc.Save(projectFullPath);
            }
        }
    }
}
