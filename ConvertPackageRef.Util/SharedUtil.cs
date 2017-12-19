using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ConvertPackageRef
{
    public static class SharedUtil
    {
        public static Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// NuGet package names are not case sensitive.
        /// </summary>
        public static readonly StringComparer NugetPackageNameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// NuGet package versions case sensitivity is not documented anywhere that could be found.  Assuming
        /// case insensitive for now.
        /// </summary>
        public static readonly StringComparer NugetPackageVersionComparer = StringComparer.OrdinalIgnoreCase;
    }
}
