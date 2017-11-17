using ConvertPackageRef;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertFromLegacy
{
    internal sealed class ConvertSolutionUtil
    {
        internal string SolutionFilePath { get; }

        internal ConvertSolutionUtil(string solutionFilePath)
        {
            SolutionFilePath = solutionFilePath;
        }

        internal void Convert()
        {
            var dir = Path.GetDirectoryName(SolutionFilePath);
            var lines = File.ReadAllLines(SolutionFilePath);
            using (var file = File.Open(SolutionFilePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(file))
            {
                foreach (var line in lines)
                {
                    if (!SolutionUtil.IsProjectLine(line))
                    {
                        writer.WriteLine(line);
                        continue;
                    }

                    var entry = SolutionUtil.ParseProjectLine(line);
                    if (entry.ProjectType != ProjectFileType.CSharp && entry.ProjectType != ProjectFileType.Basic)
                    {
                        writer.WriteLine(line);
                        continue;
                    }


                    var projectFilePath = Path.Combine(dir, entry.RelativeFilePath);
                    var projectUtil = new ProjectUtil(projectFilePath);
                    var guid = GetProjectSystemGuid(projectUtil, entry.ProjectType == ProjectFileType.CSharp);
                    if (guid == entry.TypeGuid)
                    {
                        writer.WriteLine(line);
                        continue;
                    }

                    entry = new ProjectEntry(entry.RelativeFilePath, entry.Name, entry.Guid, guid);
                    writer.WriteLine(SolutionUtil.CreateProjectLine(entry));
                }
            }
        }

        private static Guid GetProjectSystemGuid(ProjectUtil util, bool isCSharp)
        {
            if (util.IsNewSdk)
            {
                return isCSharp ? ProjectEntryUtil.NewProjectCSharpProjectType : ProjectEntryUtil.NewProjectBasicProjectType;
            }
            else 
            {
                return isCSharp ? ProjectEntryUtil.LegacyProjectCSharpProjectType : ProjectEntryUtil.LegacyProjectBasicProjectType;
            }
        }
    }
}
