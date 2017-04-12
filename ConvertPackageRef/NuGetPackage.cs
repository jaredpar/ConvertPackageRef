using System;

namespace ConvertPackageRef
{
    internal struct NuGetPackage
    {
        internal string Name { get; }
        internal string Version { get; }
        internal bool ExcludeCompile { get; }

        internal NuGetPackage(string name, string version, bool excludeCompile = false)
        { 
            Name = name;
            Version = version;
            ExcludeCompile = excludeCompile;
        }
    }
}
