﻿//using Test1Library;
using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;
using System.Diagnostics;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Msi;
using WixToolset.Dtf.WindowsInstaller;

class Constants
{
    public static string PluginVersion = "2.3.0";
}

namespace Test1.installer.wixsharp
{
    class Program
    {
#if DEBUG
        private static readonly string Configuration = "Debug";
#else
    private static readonly string Configuration = "Release";
#endif

        static string companyName = "Demo Inc.";
        static string productName = "DllExample";
        static string productVersion = "1.0.0";

        static void Main()
        {
            // issue_1739();
            // issue_1847();
            // issue_1833();
            discussions_1846();
        }

        class CustomSigner : DigitalSignature
        {
            public override int Apply(string fileToSign)
            {
                Console.WriteLine("Signing: " + fileToSign);
                return 1;
            }
        }

        static void issue_1847()
        {
            var project =
                new ManagedProject("My Product",
                    new InstallDir(@"%ProgramFiles%\My Company\My Product",
                        new Dir("Samples",
                            new File(@"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs",
                                new FileShortcut("MyApp2", @"%ProgramMenu%\My Company\My Product")))),
                    new Dir(@"%ProgramMenu%\My Company\My Product",
                        new DirectoryShortcut("Samples", "[Samples]"),
                        new ExeFileShortcut("Uninstall MyApp", "[System64Folder]msiexec.exe", "/x [ProductCode]")));

            project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
            project.UI = WUI.WixUI_ProgressOnly;

            // project.OutFileName = "setup";
            project.PreserveTempFiles = true;

            Compiler.BuildMsi(project);
        }

        static void discussions_1846()
        {
            @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\IniFile\MyProduct.msi".InsertToBinaryTable(
                "config_file",
                @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\IniFile\test.ini");
        }

        static void issue_1833()
        {
            Compiler.SignAllFilesOptions.SignEmbeddedAssemblies = true;

            Environment.CurrentDirectory = @"..\..\..\";

            var project =
                    new ManagedProject("My Product",
                        new Dir(@"%ProgramFiles%\My Company\My Product",
                            new File(
                                @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs")),
                        new Property("TEST", "TEST-VALUE"));

            project.ManagedUI = ManagedUI.DefaultWpf;
            project.SignAllFiles = true;
            project.DigitalSignature = new CustomSigner();

            project.BuildMsi();
        }

        static void issue_1832()
        {
            Environment.CurrentDirectory = @"..\..\..\";

            var project =
                    new ManagedProject("My Product",
                        new Dir(@"%ProgramFiles%\My Company\My Product",
                            new File(
                                @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs")),
                        new Property("TEST", "TEST-VALUE"));

            project.GUID = new Guid("6f330b47-2577-43ad-9095-1361ba25889b");

            project.UI = WUI.WixUI_Mondo;
            project.Version = new Version(1, 0, 0, 0);

            project.Load += e =>
            {
                MessageBox.Show(
                    $"Data[\"REMOVE\"]: {e.Data["REMOVE"]}\n" +
                    $"Session.Property(\"REMOVE\"): {e.Session.Property("REMOVE")}\n" +
                    $"IsUninstalling: {e.IsUninstalling}",
                    "WixSharp - Load");
            };

            project.AfterInstall += e =>
            {
                MessageBox.Show(
                    $"Data[\"REMOVE\"]: {e.Data["REMOVE"]}\n" +
                    $"Session.Property(\"REMOVE\"): {e.Session.Property("REMOVE")}\n" +
                    $"IsUninstalling: {e.IsUninstalling}",
                    "WixSharp - AfterInstall");
            };

            project.BuildMsi();
        }

        static void issue_1810()
        {
            Environment.CurrentDirectory = @"..\..\..\";

            var project =
                    new ManagedProject("My Product",
                        new Dir(@"%ProgramFiles%\My Company\My Product",
                            new File(
                                @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs")),
                        new Property("TEST", "TEST-VALUE"));

            project.GUID = new Guid("6f330b47-2577-43ad-9095-1361ba25889b");

            project.UI = WUI.WixUI_ProgressOnly;
            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;

            project.Version = new Version(1, 0, 0, 0);

            project.Load += (e) =>
            // project.BeforeInstall += (e) =>
            {
                var jsonTest = typeof(JObject).FullName;

                MessageBox.Show($"OnLoad: {e.Session.Property("TEST")}\n{jsonTest}", $"WixSharp - {(e.IsElevated ? "Admin" : "NonAdmin")}");
                e.Result = ActionResult.Failure;
            };

            project.DefaultRefAssemblies.Add(typeof(JObject).Assembly.Location);
            project.LoadEventExecution = EventExecution.MsiSessionScopeDeferred;

            project.OutFileName = $"MyProduct.{project.Version}";
            project.DefaultDeferredProperties += ";TEST;REMOVE;REINSTALL;Installed";
            project.PreserveTempFiles = true;

            // project.WixSourceGenerated += (doc) =>
            // {
            //     // make `<Custom Action="Set_WixSharp_Load_Action_Props" Before="AppSearch" />`
            //     // to be executed before the InstallFile action
            //     doc.FindAll("Custom")
            //         .FirstOrDefault(x => x.Attribute("Action")?.Value == "Set_WixSharp_Load_Action_Props")?
            //         .SetAttributeValue("Before", "InstallFiles");
            // };

            project.BuildMsi();
        }

        static void issue_1739()
        {
            void build(string version)
            {
                var project =
                    new ManagedProject("My Product",
                        new Dir(@"%ProgramFiles%\My Company\My Product",
                            new File(
                                @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs")));

                project.GUID = new Guid("6f330b47-2577-43ad-9095-1361ba25889b");

                project.UI = WUI.WixUI_ProgressOnly;
                project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;

                project.Version = new Version(version);

                project.AfterInstall += (e) =>
                {
                    if (e.Session.IsUpgradingInstalledVersion())
                    {
                        var isUpgrading = e.Session.IsUpgradingInstalledVersion();
                        MessageBox.Show(isUpgrading.ToString(), "");
                    }
                    // e.Result = ActionResult.Failure;
                };

                project.OutFileName = $"MyProduct.{project.Version}";
                project.DefaultDeferredProperties += ";REMOVE;REINSTALL;Installed";

                project.BuildMsi();
            }
            build("1.0.1");
            build("1.0.2");
        }

        static void Main2()
        {
            Environment.CurrentDirectory = @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\Install Files";
            var msixTemplate = @".\MyProduct.msix.xml";

            var startInfo = new ProcessStartInfo
            {
                FileName = "MsixPackagingTool.exe",
                Arguments = $@"create-package --template {msixTemplate} -v",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    string line = null;
                    while (null != (line = process.StandardOutput.ReadLine()))
                        Console.WriteLine(line);

                    string error = process.StandardError.ReadToEnd();
                    if (!error.IsEmpty())
                        Console.WriteLine(error);
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void Main1()
        {
            var project = new ManagedProject(productName,
                              new Dir(@"%ProgramFiles%\" + companyName + @"\" + productName + @"\" + productVersion,
                                  Files.FromBuildDir(@"D:\dev\support\wixsharp-issues\DllErrorExample\Contents")));

            project.GUID = new Guid("5de17d40-9e25-49fe-a835-36d7e0b64062");
            project.Version = new Version(productVersion);

            project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            project.BuildMsi();
        }

        static void issue_1727()
        {
            var project =
                new ManagedProject(
                    "My Product",
                    new Dir(@"%ProgramFiles%\My Company\My Product",
                        new File(
                            // "program.cs",
                            @"D:\dev\wixsharp4\Source\src\WixSharp.Samples\Wix# Samples\testpad\setup.cs",
                            new FileShortcut("program cs", @"%Desktop%\My Product"))
                           ),
                    new Dir(@"%Desktop%\My Product"),
                    new Property("PropName", "<your value>"));

            // project.AddUIProject("Setup1.UI"); // name of the 'Custom UI Library' project in the solution

            project.Load += (e) =>
            {
                Native.MessageBox("OnLoad", "WixSharp - .NET8");
                e.Result = ActionResult.Failure;
            };

            project.BuildMsi();
        }

        public static void Test1()
        {
            // return; // REMOVE THIS LINE TO ENABLE BUILDING

            Environment.CurrentDirectory = @"D:\dev\support\wixsharp-issues\Test1\WixSharp Setup1\WixSharp Setup1";
            Constants.PluginVersion = "2.4.0";

            string Version = Constants.PluginVersion; // READ FROM Test1 LIBRARY
            Guid ProductId = GenerateProductId("Test1" + Constants.PluginVersion);
            Guid UpgradeCode = new Guid("6476F6DF-EB27-4CAB-9790-5FE5F1C39731"); // DO NOT TOUCH

            Project project =
            new Project("Test1",
                new Media { EmbedCab = true }, // copied from old installer, don't know what it does
                CreateRevitAddinDir(2020)// ,
                                         // CreateRevitAddinDir(2021),
                                         // CreateRevitAddinDir(2022),
                                         // CreateRevitAddinDir(2023),
                                         // CreateRevitAddinDir(2024),
                                         // CreateRevitAddinDir(2025)
                       );

            project.Scope = InstallScope.perUser;

            project.Name = "Test1 Revit Plugin";
            project.ProductId = ProductId;
            project.UpgradeCode = UpgradeCode;
            //project.GUID = new Guid("6476F6DF-EB27-4CAB-9790-5FE5F1C39735");

            project.Version = new Version(Version);
            project.Description = "Revit Plugin to interact with Test1.com";
            project.ControlPanelInfo.Manufacturer = "Test1 Inc";
            project.ControlPanelInfo.ProductIcon = @".\Assets\icon.ico";
            project.ControlPanelInfo.UrlInfoAbout = "https://www.Test1.com";

            project.MajorUpgrade = new MajorUpgrade
            {
                DowngradeErrorMessage = "A newer version of Test1 Plugin is already installed.",
            };

            project.UI = WUI.WixUI_Minimal;

            project.WixVariables = new Dictionary<string, string>
            {
                { "WixUILicenseRtf", @".\Assets\License.rtf" },
                { "WixUIBannerBmp", @".\Assets\Banner.png" },
                { "WixUIDialogBmp", @".\Assets\Background.png" }
            };

            project.OutFileName = $"Test1.installer-V{Version}{(Configuration == "Debug" ? "-dev" : "")}";

            project.SourceBaseDir = @"D:\dev\support\wixsharp-issues\Test1\WixSharp Setup1\WixSharp Setup1";

            // Compiler.PreserveTempFiles = true;
            Compiler.EmitRelativePaths = false;
            Compiler.PreferredComponentGuidConsistency = ComponentGuidConsistency.WithinSingleVersion;
            // var ttt = WixGuid.NewGuid("text");

            //WixGuid.ConsistentGenerationStartValue = UpgradeCode;

            project.BuildMsi();
        }

        private static Dir CreateRevitAddinDir(int year)
        {
            string framework = GetFrameworkForYear(year);

            return new Dir($@"%AppDataFolder%\Autodesk\Revit\Addins\{year}",
                new File(@"..\Test1\Test1.addin"),
                new Dir("Test1",
                    Files.FromBuildDir($@"..\Test1\bin\{Configuration}{year}\{framework}"),
                    new Dir("Resources",
                        new Files(@"..\Test1\Resources\*.*"))));
        }

        private static Guid GenerateProductId(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));

                // Ensure the byte array is exactly 16 bytes long, as required for a GUID.
                // SHA256 generates 32 bytes, so we take the first 16 bytes.
                byte[] guidBytes = new byte[16];
                Array.Copy(hashBytes, guidBytes, 16);

                // Construct the GUID from the 16-byte array.
                return new Guid(guidBytes);
            }
        }

        private static string GetFrameworkForYear(int year)
        {
            return year < 2025 ? "net48" : "net8.0-windows";
        }
    }
}

//using System;
//using WixSharp;

//namespace WixSharp_Setup1
//{
//    public class Program
//    {
//        static void Main()
//        {
//            var project = new Project("MyProduct",
//                              new Dir(@"%ProgramFiles%\My Company\My Product",
//                                  new File("Program.cs")));

//            project.GUID = new Guid("e4c1d973-9881-498f-8b24-b61bcaee05d0");
//            //project.SourceBaseDir = "<input dir path>";
//            //project.OutDir = "<output dir path>";

//            project.BuildMsi();
//        }
//    }
//}