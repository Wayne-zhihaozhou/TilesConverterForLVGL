using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace lvglConverter.Properties
{
    /// <summary>
    /// Strongly-typed resource wrapper (auto-generated from Resources.resx).
    /// The original assembly carried an empty Resources.resx; this mirrors the
    /// decompiled Properties.Resources type so the project builds cleanly.
    /// </summary>
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCode]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    internal sealed class Resources
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    resourceMan = new ResourceManager("lvglConverter.Properties.Resources", typeof(Resources).Assembly);
                }
                return resourceMan;
            }
        }

        internal static CultureInfo Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        private Resources() { }
    }
}
