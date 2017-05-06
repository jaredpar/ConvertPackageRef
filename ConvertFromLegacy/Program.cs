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
        internal static void Main(string[] args)
        {
            var map = RepoUtil.ReadPackageVersionMap(@"e:\code\roslyn");
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
            var util = new ConvertDesktopUtil(projectFilePath, packageMap);
            util.Convert();
        }
    }
}
