using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertPackageRef
{
    public readonly struct MSBuildDocument
    {
        public static string MSBuildNamespaceUriRaw => "http://schemas.microsoft.com/developer/msbuild/2003";
        public const string NamespaceName = "mb";

        public XDocument Document { get; }
        public XmlNamespaceManager Manager { get; }
        public XNamespace Namespace { get; }

        public MSBuildDocument(XDocument document)
        {
            Document = document;
            Namespace = document.Root.Name.Namespace;
            Manager = new XmlNamespaceManager(new NameTable());
            Manager.AddNamespace(NamespaceName, Namespace == XNamespace.None ? "" : MSBuildNamespaceUriRaw);
        }

        public IEnumerable<XElement> XPathSelectElements(string localName) => Document.XPathSelectElements($"//{NamespaceName}:{localName}", Manager);
        public XElement XPathSelectElement(string localName) => Document.XPathSelectElement($"//{NamespaceName}:{localName}", Manager);
    }
}
