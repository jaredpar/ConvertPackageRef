using ConvertPackageRef;
using ConvertPackageRef.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertFromLegacy
{
    internal static class Program
    {
        private sealed class Impl
        {
            internal string RepoDir { get; }
            internal Dictionary<string, string> PackageMap { get; }

            internal Impl(string repoDir)
            {
                RepoDir = repoDir;
                PackageMap = RepoUtil.ReadPackageVersionMap(repoDir);
            }

            internal void Convert(string solutionRelativePath, bool convertProjects = true)
            {
                var solution = Path.Combine(RepoDir, solutionRelativePath);
                if (convertProjects)
                {
                    foreach (var project in SolutionUtil.ParseProjects(solution))
                    {
                        if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                        {
                            continue;
                        }

                        var filePath = Path.Combine(Path.GetDirectoryName(solution), project.RelativeFilePath);
                        ConvertProject(filePath);
                    }
                }

                var solutionUtil = new ConvertSolutionUtil(solution);
                solutionUtil.Convert();
            }

            internal void ConvertProject(string projectFilePath)
            {
                var util = new ConvertDesktopUtil(projectFilePath, PackageMap);
                util.Convert();
            }
        }

        internal static void Main(string[] args)
        {
            var impl = new Impl(@"e:\code\roslyn");
            impl.Convert("Roslyn.sln");
            impl.Convert("Compilers.sln");
            impl.Convert("CrossPlatform.sln");
            impl.Convert(@"src\Samples\Samples.sln");
        }
    }
}
