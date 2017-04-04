using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ConvertPackageRef
{
    internal static class SharedUtil
    {
        internal static string MSBuildNamespaceUriRaw => "http://schemas.microsoft.com/developer/msbuild/2003";
        internal static Uri MSBuildNamespaceUri { get; } = new Uri(MSBuildNamespaceUriRaw);
        internal static XNamespace MSBuildNamespace { get; } = XNamespace.Get(MSBuildNamespaceUriRaw);
        internal static Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// NuGet package names are not case sensitive.
        /// </summary>
        internal static readonly StringComparer NugetPackageNameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// NuGet package versions case sensitivity is not documented anywhere that could be found.  Assuming
        /// case insensitive for now.
        /// </summary>
        internal static readonly StringComparer NugetPackageVersionComparer = StringComparer.OrdinalIgnoreCase;

        internal struct IgnoreGenerateNameComparer : IEqualityComparer<NuGetPackage>
        {
            public bool Equals(NuGetPackage x, NuGetPackage y) =>
                x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase) &&
                x.Version.Equals(y.Version, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(NuGetPackage obj) => obj.GetHashCode();
        }
    }
}
