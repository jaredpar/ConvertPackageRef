using ConvertPackageRef;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace QuickChange
{
    internal sealed class Converter
    {
        internal readonly struct ProjectRenameData
        {
            internal string OldProjectName { get; }
            internal string NewProjectName { get; }
            internal string NewAssemblyName { get; }

            internal ProjectRenameData(string oldProjectName, string newProjectName, string newAssemblyName)
            {
                OldProjectName = oldProjectName;
                NewProjectName = newProjectName;
                NewAssemblyName = newAssemblyName;
            }

            public override string ToString() => $"{OldProjectName} -> {NewProjectName}";
        }

        /// <summary>
        /// Map of old project name to new project name & assembly name.
        /// </summary>
        internal static string[] InitialProjectRenameData { get; } = new string[]
        {
            "BasicCommandLineTest#Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests",
            "BasicCompilerEmitTest#Microsoft.CodeAnalysis.VisualBasic.Emit.UnitTests",
            "BasicCompilerSemanticTest#Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests",
            "BasicCompilerSymbolTest#Microsoft.CodeAnalysis.VisualBasic.Symbol.UnitTests",
            "BasicCompilerSyntaxTest#Microsoft.CodeAnalysis.VisualBasic.Syntax.UnitTests",
            "BasicCompilerTestUtilities#Microsoft.CodeAnalysis.VisualBasic.Test.Utilities",
            "BasicEditorServicesTest#Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests",
            "BasicExpressionCompiler#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler#Microsoft.CodeAnalysis.VisualBasic.EE.EC",
            "BasicExpressionCompilerTest#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler.UnitTests#Microsoft.CodeAnalysis.VisualBasic.EE.EC.UnitTests",
            "BasicResultProvider#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider#Microsoft.CodeAnalysis.VisualBasic.EE.RP",
            "BasicResultProvider.Portable#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider#Microsoft.CodeAnalysis.VisualBasic.EE.RP",
            "BasicResultProviderTest#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider.UnitTests#Microsoft.CodeAnalysis.VisualBasic.EE.RP.UnitTests",
            "CSharpCommandLineTest#Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests",
            "CSharpCompilerEmitTest#Microsoft.CodeAnalysis.CSharp.Emit.UnitTests",
            "CSharpCompilerSemanticTest#Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests",
            "CSharpCompilerSymbolTest#Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests",
            "CSharpCompilerSyntaxTest#Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests",
            "CSharpCompilerTestUtilities#Microsoft.CodeAnalysis.CSharp.Test.Utilities",
            "CSharpEditorServicesTest#Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests",
            "CSharpEditorServicesTest2#Microsoft.CodeAnalysis.CSharp.EditorFeatures2.UnitTests",
            "CSharpExpressionCompiler#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler#Microsoft.CodeAnalysis.CSharp.EE.EC",
            "CSharpExpressionCompilerTest#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler.UnitTests#Microsoft.CodeAnalysis.CSharp.EE.EC.UnitTests",
            "CSharpResultProvider.Portable#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider#Microsoft.CodeAnalysis.CSharp.EE.RP",
            "CSharpResultProviderTest#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider.UnitTests#Microsoft.CodeAnalysis.CSharp.EE.RP.UnitTests",
            "CSharpServicesTest#Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests",
            "CSharpVisualStudioTest#Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests",
            "CSharpWinRTTest#Microsoft.CodeAnalysis.CSharp.WinRT.UnitTests",
            "CodeAnalysisTest#Microsoft.CodeAnalysis.UnitTests",
            "CompilerTestResources#Microsoft.CodeAnalysis.Compiler.Test.Resources",
            "EditorServicesTest#Microsoft.CodeAnalysis.EditorFeatures.UnitTests",
            "EditorServicesTest2#Microsoft.CodeAnalysis.EditorFeatures2.UnitTests",
            "ExpressionCompiler#Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompiler#Microsoft.CodeAnalysis.EE.EC",
            "ExpressionCompilerTestUtilities#Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompiler.Utilities#Microsoft.CodeAnalysis.EE.EC.Utilities",
            "FunctionResolver#Microsoft.CodeAnalysis.ExpressionEvaluator.FunctionResolver#Microsoft.CodeAnalysis.EE.FR",
            "FunctionResolverTest#Microsoft.CodeAnalysis.ExpressionEvaluator.FunctionResolver.UnitTests#Microsoft.CodeAnalysis.EE.FR.UnitTests",
            "InteractiveHostTest#InteractiveHost.UnitTests",
            "MSBuildTaskTests#Microsoft.Build.Tasks.CodeAnalysis.UnitTetss",
            "ResultProvider.Portable#Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider#Microsoft.CodeAnalysis.EE.RP",
            "ResultProviderTestUtilities#Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider.Utilities#Microsoft.CodeAnalysis.EE.RP.Utilities",
            "ServicesTest#Microsoft.CodeAnalysis.Workspaces.UnitTests",
            "ServicesTestUtilities#Microsoft.CodeAnalysis.EditorFeatures.Utilities",
            "ServicesTestUtilities2#Microsoft.CodeAnalysis.EditorFeatures.Test.Utilities",
            "ServicesVisualStudioTest#Microsoft.VisualStudio.LanguageServices.UnitTests",
            "TestUtilities#Microsoft.CodeAnalysis.Test.Utilities",
            "VBCSCompilerTests#VBCSCompiler.UnitTests",
            "VisualBasicServicesTest#Microsoft.CodeAnalysis.VisualBasic.Workspaces.UnitTests",
            "VisualStudioIntegrationTestUtilities#Microsoft.VisualStudio.LanguageServices.IntegrationTests.Utilities",
            "VisualStudioIntegrationTests#Microsoft.VisualStudio.LanguageServices.IntegrationTests",
            "VisualStudioTestUtilities2#Microsoft.VisualStudio.LanguageServices.Test.Utilities2",
        };

        internal static string[] ProjectExcludeData { get; } = new string[]
        {
            "ResultProvider.NetFX20",
            "CSharpResultProvider.NetFX20",
            "BasicResultProvider.NetFX20",
        };

        internal static HashSet<string> ExcludedProjectSet { get; } = new HashSet<string>(ProjectExcludeData, StringComparer.OrdinalIgnoreCase);

        internal static Dictionary<string, ProjectRenameData> InitialProjectRenameMap { get; }

        static Converter()
        {
            InitialProjectRenameMap = new Dictionary<string, ProjectRenameData>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in InitialProjectRenameData)
            {
                var all = line.Split('#');
                var oldName = all[0];
                var newAssemblyName = all[1];
                var newProjectName = all.Length == 2 ? all[1] : all[2];
                InitialProjectRenameMap[oldName] = new ProjectRenameData(oldName, newProjectName, newAssemblyName);
            }
        }

        /// <summary>
        /// Map of old to new project names
        /// </summary>
        internal Dictionary<string, string> ProjectNameMap { get; }

        /// <summary>
        /// Map of old to new assembly names (no extension)
        /// </summary>
        internal Dictionary<string, string> AssemblyNameMap { get; }

        internal Converter(string solutionFilePath)
        {
            ProjectNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AssemblyNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in SolutionUtil.ParseLanguageProjectsRooted(solutionFilePath))
            {
                if (ExcludedProjectSet.Contains(entry.Name))
                {
                    continue;
                }

                var util = new ProjectUtil(entry.FilePath);
                var assemblyName = util.AssemblyNameWithoutExtension;
                var name = entry.Name;
                if (InitialProjectRenameMap.TryGetValue(name, out var data))
                {
                    ProjectNameMap[name] = data.NewProjectName;
                    AssemblyNameMap[assemblyName] = data.NewAssemblyName;
                }
                else
                {
                    ProjectNameMap[name] = assemblyName;
                }
            }
        }

        internal void ConvertProjectFile(string projectFilePath)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var util = new ProjectUtil(projectFilePath);
            ConvertProjectReferences(util);
            ConvertInternalsVisibleTo(util);

            var saveName = util.FilePath;
            if (ProjectNameMap.TryGetValue(util.ProjectName, out var newProjectName))
            {
                var assemblyName = util.AssemblyNameWithoutExtension;
                if (!AssemblyNameMap.TryGetValue(assemblyName, out var newAssemblyName))
                {
                    newAssemblyName = assemblyName;
                }

                var assemblyNameElem = util.MSBuildDocument.XPathSelectElement("AssemblyName");
                if (comparer.Equals(newAssemblyName, newProjectName))
                {
                    assemblyNameElem?.Remove();
                }
                else
                {
                    if (assemblyNameElem == null)
                    {
                        assemblyNameElem = new XElement(util.MSBuildDocument.Namespace.GetName("AssemblyName"));
                        util.GetOrCreateMainPropertyGroup().AddFirst(assemblyNameElem);
                    }

                    assemblyNameElem.Value = newAssemblyName;
                }

                if (!StringComparer.OrdinalIgnoreCase.Equals(util.ProjectName, newProjectName))
                {
                    File.Delete(util.FilePath);
                    saveName = ChangeFileName(util.FilePath, newProjectName);
                }
            }

            util.Document.Save(saveName);
        }

        private void ConvertProjectReferences(ProjectUtil util)
        {
            foreach (var refElement in util.MSBuildDocument.XPathSelectElements("ProjectReference"))
            {
                var includeAttr = refElement.Attribute("Include");
                var include = includeAttr.Value;
                var name = Path.GetFileNameWithoutExtension(include);
                if (ProjectNameMap.TryGetValue(name, out var newName))
                {
                    includeAttr.SetValue(ChangeFileName(include, newName));
                }
            }
        }

        private void ConvertInternalsVisibleTo(ProjectUtil util)
        {
            foreach (var itemGroup in util.MSBuildDocument.XPathSelectElements("ItemGroup"))
            {
                foreach (var element in itemGroup.Elements())
                {
                    if (element.Name.LocalName.StartsWith("InternalsVisibleTo"))
                    {
                        var include = element.Attribute("Include");
                        var name = include.Value;
                        if (AssemblyNameMap.TryGetValue(name, out var newName))
                        {
                            include.SetValue(newName);
                        }
                    }
                }
            }
        }

        private static string ChangeFileName(string path, string newFileName)
        {
            var dir = Path.GetDirectoryName(path);
            var oldFileExt = Path.GetExtension(path);
            return Path.Combine(dir, newFileName + oldFileExt);
        }

        internal void ConvertProjectFiles(string solutionFilePath)
        {
            foreach (var entry in SolutionUtil.ParseLanguageProjectsRooted(solutionFilePath))
            {
                ConvertProjectFile(entry.FilePath);
            }
        }

        internal void ConvertSolutionEntries(string solutionFilePath)
        {
            var oldLines = File.ReadAllLines(solutionFilePath);
            var newLines = new List<string>();
            foreach (var line in oldLines)
            {
                if (SolutionUtil.IsProjectLine(line))
                {
                    var entry = SolutionUtil.ParseProjectLine(line);
                    if (ProjectNameMap.TryGetValue(entry.Name, out var newName))
                    {
                        var newEntry = new ProjectEntry(ChangeFileName(entry.RelativeFilePath, newName), newName, entry.Guid, entry.TypeGuid);
                        newLines.Add(SolutionUtil.CreateProjectLine(newEntry));
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(solutionFilePath, newLines);
        }

    }



}
