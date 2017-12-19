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
            var map = getProjectNameMap(@"e:\code\roslyn\Roslyn.sln");
            convertProjects(@"e:\code\roslyn\Roslyn.sln");
            convertSolutionEntries(@"e:\code\roslyn\Roslyn.sln");
            convertSolutionEntries(@"e:\code\roslyn\Compilers.sln");

            Dictionary<string, string> getProjectNameMap(string solutionFilePath)
            {
                var local = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var newNames = new HashSet<string>();
                foreach (var line in ProjectMap)
                {
                    var both = line.Split('#');
                    var oldName = both[0];
                    var newName = both[1];
                    if (!newNames.Add(newName))
                    {
                        throw new Exception($"Duplicate name entries {newName}");
                    }

                    local[oldName] = newName;
                }

                foreach (var entry in SolutionUtil.ParseLanguageProjects(solutionFilePath))
                {
                    if (entry.Name.Contains("NetFX20"))
                    {
                        continue;
                    }

                    if (!local.TryGetValue(entry.Name, out var newName))
                    {
                        var util = new ProjectUtil(entry.GetFullPath(solutionFilePath));
                        var assemblyName = util.AssemblyNameWithoutExtension;
                        if (!newNames.Add(assemblyName))
                        {
                            throw new Exception($"Duplicate name entries {assemblyName}");
                        }
                        local[entry.Name] = assemblyName;
                    }
                }

                return local;
            }

            string changeFileName(string path, string newFileName)
            {
                var dir = Path.GetDirectoryName(path);
                var oldFileExt = Path.GetExtension(path);
                return Path.Combine(dir, newFileName + oldFileExt);
            }

            void convertProject(ProjectUtil util)
            {
                foreach (var refElement in util.MSBuildDocument.XPathSelectElements("ProjectReference"))
                {
                    var includeAttr = refElement.Attribute("Include");
                    var include = includeAttr.Value;
                    var name = Path.GetFileNameWithoutExtension(include);
                    if (map.TryGetValue(name, out var newName))
                    {
                        includeAttr.SetValue(changeFileName(include, newName));
                    }
                }

                if (map.TryGetValue(util.ProjectName, out var newProjectName))
                {
                    var assemblyNameElem = util.MSBuildDocument.XPathSelectElement("AssemblyName");
                    assemblyNameElem?.Remove();

                    if (util.ProjectName != newProjectName)
                    {
                        File.Delete(util.FilePath);
                        util.Document.Save(changeFileName(util.FilePath, newProjectName));
                    }
                }
                else
                {
                    util.Document.Save(util.FilePath);
                }
            }

            void convertProjects(string solutionFilePath)
            {
                foreach (var entry in SolutionUtil.ParseProjects(solutionFilePath))
                {
                    if (entry.ProjectType != ProjectFileType.CSharp && entry.ProjectType != ProjectFileType.Basic)
                    {
                        continue;
                    }
                    var projectUtil = new ProjectUtil(entry.GetFullPath(solutionFilePath));
                    convertProject(projectUtil);
                }

            }

            void convertSolutionEntries(string solutionFilePath)
            {
                var oldLines = File.ReadAllLines(solutionFilePath);
                var newLines = new List<string>();
                foreach (var line in oldLines)
                {
                    if (SolutionUtil.IsProjectLine(line))
                    {
                        var entry = SolutionUtil.ParseProjectLine(line);
                        if (map.TryGetValue(entry.Name, out var newName))
                        {
                            var newEntry = new ProjectEntry(changeFileName(entry.RelativeFilePath, newName), newName, entry.Guid, entry.TypeGuid);
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

        private static readonly string[] ProjectMap = new string[]
        {
            "CodeAnalysisTest#Microsoft.CodeAnalysis.UnitTests",
            "VBCSCompilerTests#VBCSCompiler.UnitTests",
            "CSharpCommandLineTest#Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests",
            "CSharpCompilerEmitTest#Microsoft.CodeAnalysis.CSharp.Emit.UnitTests",
            "CSharpCompilerSemanticTest#Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests",
            "CSharpCompilerSymbolTest#Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests",
            "CSharpCompilerSyntaxTest#Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests",
            "CSharpWinRTTest#Microsoft.CodeAnalysis.CSharp.WinRT.UnitTests",
            "CompilerTestResources#Microsoft.CodeAnalysis.Compiler.Test.Resources",
            "CSharpCompilerTestUtilities#Microsoft.CodeAnalysis.CSharp.Test.Utilities",
            "BasicCompilerTestUtilities#Microsoft.CodeAnalysis.VisualBasic.Test.Utilities",
            "BasicCommandLineTest#Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests",
            "BasicCompilerEmitTest#Microsoft.CodeAnalysis.VisualBasic.Emit.UnitTests",
            "BasicCompilerSemanticTest#Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests",
            "BasicCompilerSymbolTest#Microsoft.CodeAnalysis.VisualBasic.Symbol.UnitTests",
            "BasicCompilerSyntaxTest#Microsoft.CodeAnalysis.VisualBasic.Syntax.UnitTests",
            "ServicesTest#Microsoft.CodeAnalysis.Workspaces.UnitTests",
            "CSharpServicesTest#Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests",
            "VisualBasicServicesTest#Microsoft.CodeAnalysis.VisualBasic.Workspaces.UnitTests",
            "BasicEditorServicesTest#Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests",
            "CSharpEditorServicesTest#Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests",
            "CSharpEditorServicesTest2#Microsoft.CodeAnalysis.CSharp.EditorFeatures2.UnitTests",
            "EditorServicesTest#Microsoft.CodeAnalysis.EditorFeatures.UnitTests",
            "EditorServicesTest2#Microsoft.CodeAnalysis.EditorFeatures2.UnitTests",
            "ServicesTestUtilities#Microsoft.CodeAnalysis.EditorFeatures.Utilities",
            "InteractiveHostTest#InteractiveHost.UnitTests",
            "CSharpVisualStudioTest#Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests",
            "ServicesVisualStudioTest#Microsoft.VisualStudio.LanguageServices.UnitTests",
            "CSharpExpressionCompilerTest#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler.UnitTests",
            "CSharpResultProviderTest#Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider.UnitTests",
            "BasicExpressionCompilerTest#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler.UnitTests",
            "BasicResultProviderTest#Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider.UnitTests",
            "ExpressionCompilerTestUtilities#Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompiler.Utilities",
            "ResultProviderTestUtilities#Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider.Utilities",
            "MSBuildTaskTests#Microsoft.Build.Tasks.CodeAnalysis.UnitTetss",
            "BasicResultProvider.NetFX20#BasicResultProvider.NetFX20.dll",
            "CSharpResultProvider.NetFX20#CSharpResultProvider.NetFX20.dll",
            "TestUtilities#Microsoft.CodeAnalysis.Test.Utilities",
            "VisualStudioIntegrationTests#Microsoft.VisualStudio.LanguageServices.IntegrationTests",
            "VisualStudioIntegrationTestUtilities#Microsoft.VisualStudio.LanguageServices.IntegrationTests.Utilities",
            "ServicesTestUtilities2#Microsoft.CodeAnalysis.EditorFeatures.Test.Utilities",
            "VisualStudioTestUtilities2#Microsoft.VisualStudio.LanguageServices.Test.Utilities2",
        };

    }
}
