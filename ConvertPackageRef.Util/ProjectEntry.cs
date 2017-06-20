using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertPackageRef
{
    /// <summary>
    /// All of the project entry contained in a solution file.
    /// </summary>
    public struct ProjectEntry
    {
        public string RelativeFilePath { get; }
        public string Name { get; }
        public Guid Guid { get; }
        public Guid TypeGuid { get; }

        public bool IsFolder => TypeGuid == ProjectEntryUtil.FolderProjectType;
        public ProjectFileType ProjectType => ProjectEntryUtil.GetProjectFileType(RelativeFilePath);

        public ProjectEntry(
            string relativeFilePath,
            string name,
            Guid guid,
            Guid typeGuid)
        {
            RelativeFilePath = relativeFilePath;
            Name = name;
            Guid = guid;
            TypeGuid = typeGuid;
        }

        public override string ToString() => Name;
    }

    public static class ProjectEntryUtil
    {
        public static readonly Guid FolderProjectType = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
        public static readonly Guid VsixProjectType = new Guid("{82B43B9B-A64C-4715-B499-D71E9CA2BD60}");
        public static readonly Guid LegacyProjectCSharpProjectType = new Guid("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
        public static readonly Guid LegacyProjectBasicProjectType = new Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");
        public static readonly Guid NewProjectCSharpProjectType = new Guid("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}");
        public static readonly Guid NewProjectBasicProjectType = new Guid("{778DAE3C-4631-46EA-AA77-85C1314464D9}");

        public static ProjectFileType GetProjectFileType(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csproj": return ProjectFileType.CSharp;
                case ".vbproj": return ProjectFileType.Basic;
                case ".shproj": return ProjectFileType.Shared;
                default:
                    return ProjectFileType.Unknown;
            }
        }

    }
}
