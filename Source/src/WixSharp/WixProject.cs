using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace WixSharp
{
    /// <summary>
    /// Base class for WiX projects (e.g. Project, Bundle).
    /// </summary>
    public abstract partial class WixProject : WixEntity
    {
        string sourceBaseDir = "";

        /// <summary>
        /// Base directory for the relative paths of the bootstrapper items (e.g. <see cref="T:WixSharp.Bootstrapper.MsiPackage"></see>).
        /// </summary>
        public string SourceBaseDir
        {
            get { return sourceBaseDir.ExpandEnvVars(); }
            set { sourceBaseDir = value; }
        }

        /// <summary>
        /// The location of the config file for Managed Custom Action.
        /// <para>The config file (CustomAction.config) is the file to be passed to the MakeSfxCA.exe when packing the Custom Action assembly.</para>
        /// </summary>
        public string CAConfigFile = "";

        /// <summary>
        /// Resolves user defined config file.
        /// </summary>
        /// <value>The custom action config.</value>
        internal string CustomActionConfig
        {
            get
            {
                var configFile = this.CAConfigFile;
                if (configFile.IsNotEmpty() && !System.IO.Path.IsPathRooted(configFile))
                    return Utils.PathCombine(this.SourceBaseDir, this.CAConfigFile);

                return configFile;
            }
        }

        /// <summary>
        /// Name of the MSI/MSM file (without extension) to be build.
        /// </summary>
        public string OutFileName = "setup";

        string outDir;

        /// <summary>
        /// Gets a value indicating whether the OutDir is set.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is out dir set; otherwise, <c>false</c>.
        /// </value>
        public bool IsOutDirSet => !outDir.IsEmpty();

        /// <summary>
        /// The output directory. The directory where all msi and temporary files should be assembled. The <c>CurrentDirectory</c> will be used if <see cref="OutDir"/> is left unassigned.
        /// </summary>
        public string OutDir
        {
            get
            {
                return (outDir.IsEmpty() ? Environment.CurrentDirectory : outDir.ExpandEnvVars()).EnsureDirExists(); ;
            }
            set
            {
                outDir = value;
            }
        }

        /// <summary>
        /// Collection of XML namespaces (e.g. <c>xmlns:iis="http://wixtoolset.org/schemas/v4/wxs/iis"</c>) to be declared in the XML (WiX project) root.
        /// </summary>
        public List<string> WixNamespaces = new List<string>();

        /// <summary>
        /// Collection of paths to the WiX extensions.
        /// </summary>
        public List<string> WixExtensions
        {
            get
            {
                return wixExtensions;
            }
        }

        List<string> wixExtensions = new List<string>();

        /// <summary>
        /// Collection of paths to the external wsx files containing Fragment(s).
        /// <para>
        /// At the compile time files will be pases to candle.exe but the referencing them fragments in the primary wxs (XML)
        /// needs to be done from WixSourceGenerated event handler.
        /// </para>
        /// </summary>
        public List<string> WxsFiles = new List<string>();

        /// <summary>
        /// This element exposes advanced WiX functionality. Use this element to declare WiX variables from directly within your
        /// authoring. WiX variables are not resolved until the final msi/msm/pcp file is actually generated. WiX variables do not
        /// persist into the msi/msm/pcp file, so they cannot be used when an MSI file is being installed; it's a WiX-only concept.
        /// </summary>
        public Dictionary<string, string> WixVariables = new Dictionary<string, string>();

        /// <summary>
        /// Collection of paths to the external wsxlib files to be passed to the Light linker.
        /// </summary>
        public List<string> LibFiles = new List<string>();

        internal bool IsLocalized
        {
            get { return (Language.ToLower() != "en-us" && Language.ToLower() != "en") || !LocalizationFile.IsEmpty(); }
        }

        internal bool IsNonEnglish
        {
            get { return (Language.ToLower() != "en-us" && Language.ToLower() != "en"); }
        }

        /// <summary>
        /// Path to the Localization file.
        /// </summary>
        public string LocalizationFile = "";

        /// <summary>
        /// Gets a value indicating whether this project supports multiple languages.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this project is multi language; otherwise, <c>false</c>.
        /// </value>
        public bool IsMultiLanguage => Language.Split(',', ';').Count() > 1;

        /// <summary>
        /// Gets the default language
        /// </summary>
        /// <value>
        /// The default language.
        /// </value>
        public CultureInfo DefaultLanguage
        {
            get
            {
                var lng = Language.Split(',', ';').FirstOrDefault();
                if (lng == null)
                    return null;
                else
                    return new CultureInfo(lng);
            }
        }

        string language = "en-US";

        /// <summary>
        /// Installation UI Language. If not specified <c>"en-US"</c> will be used.
        /// <para>It is possible to specify multiple languages separated by coma or semicolon. All extra languages will be used
        /// as additional values for 'Package.Languages' attribute and light.exe '-cultures:' command line parameters.</para>
        /// </summary>
        public string Language
        {
            get { return language; }
            set
            {
                language = value.DeflateWhitespaces();
                try
                {
                    if (language.IsNotEmpty())
                        Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
                }
                catch { }
            }
        }

        /// <summary>
        /// The Encoding to be used for generating WSX. If not specified the
        /// <c>System.Text.Encoding.UTF8</c> will be used.
        /// </summary>
        public Encoding Encoding = Encoding.UTF8;

        /// <summary>
        /// WiX linker <c>Light.exe</c> options (e.g. "-sice:ICE30 -sw1076" (disable warning 1076 and ICE warning 30).
        /// </summary>
        [Obsolete("WiX4 (and higher) does not use Candle/Light any more but wix.exe. Thus use WixOptions instead", true)]
        public string LightOptions = "";

        /// <summary>
        /// WiX compiler <c>Candle.exe</c> options (e.g. "-sw1076" to disable warning 1026).
        /// </summary>
        [Obsolete("WiX4 (and higher) does not use Candle/Light any more but wix.exe. Thus use WixOptions instead", true)]
        public string CandleOptions = "";

        /// <summary>
        /// WiX compiler <c>wix.exe</c> options (e.g. "-define DEBUG"). Available only in WiX v4.*
        /// </summary>
        public string WixOptions = "";

        /// <summary>
        /// Occurs when WiX source code generated. Use this event if you need to modify generated XML (XDocument)
        /// before it is compiled into MSI.
        /// </summary>
        public event XDocumentGeneratedDlgt WixSourceGenerated;

        /// <summary>
        /// Occurs when `wix.exe` build command generated.
        /// Use this event if you want to modify the command before
        /// it is executed by `wix.exe`.
        /// </summary>
        public event WixBuildCommandGeneratedDlgt WixBuildCommandGenerated = x => x;

        internal string OnWixBuildCommandGenerated(string command) => WixBuildCommandGenerated?.Invoke(command) ?? command;

        internal string SetVersionFromIdValue = "";

        /// <summary>
        /// Occurs when WiX source file is saved. Use this event if you need to do any post-processing of the generated/saved file.
        /// </summary>
        public event XDocumentSavedDlgt WixSourceSaved;

        /// <summary>
        /// Occurs when WiX source file is formatted and ready to be saved. Use this event if you need to do any custom formatting
        /// of the XML content before it is saved by the compiler.
        /// </summary>
        public event XDocumentFormatedDlgt WixSourceFormated;

        /// <summary>
        /// Forces <see cref="Compiler"/> to preserve all temporary build files (e.g. *.wxs).
        /// <para>The default value is <c>false</c>: all temporary files are deleted at the end of the build/compilation.</para>
        /// <para>Note: if <see cref="Compiler"/> fails to build MSI the <c>PreserveTempFiles</c>
        /// value is ignored and all temporary files are preserved.</para>
        /// </summary>
        public bool PreserveTempFiles = false;

        /// <summary>
        /// Forces <see cref="Compiler"/> to preserve all obj/pdb build files (e.g. *.wixobj and *.wixpdb).
        /// <para>The default value is <c>false</c>: all temporary files are deleted at the end of the build/compilation.</para>
        /// </summary>
        public bool PreserveDbgFiles = false;

        /// <summary>
        /// Invokes the WixSourceGenerated event handlers.
        /// </summary>
        /// <param name="doc">The XML document.</param>
        internal void InvokeWixSourceGenerated(XDocument doc)
        {
            foreach (var item in this.WixFragments)
                doc.Select(item.Key)
                   .AddWixFragment(item.Value.ToArray());

            WixSourceGenerated?.Invoke(doc);
        }

        internal Dictionary<string, List<XElement>> WixFragments = new Dictionary<string, List<XElement>>();

        /// <summary>
        /// Adds the specified XML content as a WiX Fragment/FragmentRef elements combination.
        /// </summary>
        /// <param name="placementPath">The placement path to the element matching the specified path (e.g. <c>Select("Product/Package")</c>.</param>
        /// <param name="content">The XML fragment content.</param>
        /// <returns></returns>
        public WixProject AddWixFragment(string placementPath, params XElement[] content)
        {
            if (WixFragments.ContainsKey(placementPath))
                WixFragments[placementPath].AddRange(content);
            else
                WixFragments.Add(placementPath, content.ToList());

            return this;
        }

        /// <summary>
        /// Adds the specified XML content as a WiX Fragment/FragmentRef elements combination.
        /// </summary>
        /// <param name="placementPath">The placement path to the element matching the specified path (e.g. <c>Select("Product/Package")</c>.</param>
        /// <param name="content">The XML fragment content. Collection of objects that can be converted to XML with ToXML() call</param>
        /// <returns></returns>
        public WixProject AddWixFragment(string placementPath, params IXmlAware[] content)
        {
            return AddWixFragment(placementPath, content.Select(x => x.ToXml()).ToArray());
        }

        /// <summary>
        /// Invokes the WixSourceSaved event handlers.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        internal void InvokeWixSourceSaved(string fileName)
        {
            if (WixSourceSaved != null)
                WixSourceSaved(fileName);
        }

        /// <summary>
        /// Invokes the WixSourceFormated event handlers.
        /// </summary>
        /// <param name="content">The content.</param>
        internal void InvokeWixSourceFormated(ref string content)
        {
            if (WixSourceFormated != null)
                WixSourceFormated(ref content);
        }

        /// <summary>
        /// The custom algorithm for generating WiX elements Id(s).
        /// <para>If the custom algorithm is used to concurrently build multiple projects and access
        /// resources, then you may consider built-in synchronization available with
        /// <see cref="T:WixSharp.Compiler.IsWxsGenerationThreadSafe"/>.</para>
        /// </summary>
        ///<example>The following code demonstrates how to generate File Id(s) based is on the hash
        /// of the target path of the file being installed.
        ///<code>
        /// WixEntity.CustomIdAlgorithm =
        ///       entity =>
        ///       {
        ///           if (entity is File file)
        ///           {
        ///               var target_path = project.GetTargetPathOf(file);
        ///               var hash = target_path.GetHashCode32();
        ///
        ///               // WiX does not allow '-' char in Id. So need to use `Math.Abs`
        ///               return $"{target_path.PathGetFileName()}_{Math.Abs(hash)}";
        ///           }
        ///
        ///           return null; // pass to default ID generator
        ///       };
        /// </code>
        /// </example>
        public Func<WixEntity, string> CustomIdAlgorithm = null;
    }
}