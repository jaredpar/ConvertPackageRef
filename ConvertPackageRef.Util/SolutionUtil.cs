using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertPackageRef
{
    public static class SolutionUtil
    {
        public static List<ProjectEntry> ParseProjects(string solutionPath)
        {
            using (var reader = new StreamReader(solutionPath))
            {
                var list = new List<ProjectEntry>();
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (!IsProjectLine(line))
                    {
                        continue;

                    }

                    list.Add(ParseProjectLine(line));
                }
                return list;
            }
        }

        public static IEnumerable<ProjectEntry> ParseLanguageProjects(string solutionPath) => ParseProjects(solutionPath).Where(e => e.ProjectType == ProjectFileType.Basic || e.ProjectType == ProjectFileType.CSharp);

        public static bool IsProjectLine(string line) => line.StartsWith("Project");

        public static ProjectEntry ParseProjectLine(string line)
        {
            var index = 0;
            var typeGuid = ParseStringLiteral(line, ref index);
            var name = ParseStringLiteral(line, ref index);
            var filePath = ParseStringLiteral(line, ref index);
            var guid = ParseStringLiteral(line, ref index);
            return new ProjectEntry(
                relativeFilePath: filePath,
                name: name,
                guid: Guid.Parse(guid),
                typeGuid: Guid.Parse(typeGuid));
        }

        public static string CreateProjectLine(ProjectEntry entry)
        {
            var typeGuid = entry.TypeGuid.ToString("B").ToUpper();
            var guid = entry.Guid.ToString("B").ToUpper();
            return $@"Project(""{typeGuid}"") = ""{entry.Name}"", ""{entry.RelativeFilePath}"", ""{guid}""";
        }

        private static string ParseStringLiteral(string line, ref int index)
        {
            var start = line.IndexOf('"', index);
            if (start < 0)
            {
                goto error;
            }

            start++;
            var end = line.IndexOf('"', start);
            if (end < 0)
            {
                goto error;
            }

            index = end + 1;
            return line.Substring(start, end - start);

        error:
            throw new Exception($"Invalid project line {line}");
        }
    }
}
