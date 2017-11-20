//css_dir ..\..\;
//css_ref Wix_bin\SDK\Microsoft.Deployment.WindowsInstaller.dll;
//css_ref System.Core.dll;
using Microsoft.Win32;
using System;
using System.Linq;
using WixSharp;

class Script
{
    static public void Main()
    {
        AutoId_TargetPathHash_BuiltIn();
    }

    static public void ExplicitId()
    {
        var project =
           new Project("MyProduct",
               new Dir(new Id("PRODUCT_INSTALLDIR"), @"%ProgramFiles%\My Company\My Product",
                   new File(new Id("App_File"), @"Files\Bin\MyApp.exe"),
                   new Dir(@"Docs\Manual",
                       new File(new Id("Manual_File"), @"Files\Docs\Manual.txt"))));

        project.PreserveTempFiles = true;
        project.UI = WUI.WixUI_ProgressOnly;
        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        Compiler.BuildMsi(project);
    }

    static public void ExplicitId_AlternativeSyntax()
    {
        // arguably less inconvenient
        var project =
             new Project("MyProduct",
                 new Dir(@"%ProgramFiles%\My Company\My Product",
                     new File(@"Files\Bin\MyApp.exe") { Id = "App_File" },
                     new Dir(@"Docs\Manual",
                         new File(@"Files\Docs\Manual.txt") { Id = "Manual_File" })));

        // Note: the first dir in the project constructor is converted
        // in the sequence of nested dirs based on the specified 'path'.
        // Where the first dir is %ProgramFiles% and the last one is 'My Product'.
        // We need to set the Id to the 'My Product'
        project.AllDirs.Single(d => d.Name == "My Product").Id = "PRODUCT_INSTALLDIR";

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        Compiler.BuildWxs(project);
    }

    static public void AutoId_Incremental_BuiltIn()
    {
        var project =
             new Project("MyProduct",
                 new Dir(@"%ProgramFiles%\My Company\My Product",
                     new File(@"Files\Bin\MyApp.exe"),
                     new Dir(@"Docs\Manual",
                         new File(@"Files\Docs\Manual.txt"))));

        // globally via configuration
        Compiler.AutoGeneration.LegacyDefaultIdAlgorithm = true;

        // globally via delegate
        Compiler.AutoGeneration.CustomIdAlgorithm = WixEntity.IncrementalIdFor;

        // for project only via delegate
        project.CustomIdAlgorithm = WixEntity.IncrementalIdFor;

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        Compiler.BuildWxs(project);
    }

    static public void AutoId_TargetPathHash_BuiltIn()
    {
        // This algorithm addresses the limitation of the incremental-Id legacy algorithm,
        // which it quite reliable but non-deterministic.
        // Thus the code below generates the following XML:
        // <File Id="Manual.txt_90490314" Source="Files\Docs\Manual.txt" />

        var project =
             new Project("MyProduct",
                 new Dir(@"%ProgramFiles%\My Company\My Product",
                     new File(@"Files\Bin\MyApp.exe"),
                     new Dir(@"Docs\Manual",
                         new File(@"Files\Docs\Manual.txt"))));

        // globally via delegate
        Compiler.AutoGeneration.CustomIdAlgorithm = project.HashedTargetPathIdAlgorithm;

        // You need to adjust IsWxsGenerationThreadSafe only if you do concurrent MSI builds
        // Compiler.AutoGeneration.IsWxsGenerationThreadSafe = true;

        // globally via configuration
        Compiler.AutoGeneration.LegacyDefaultIdAlgorithm = false;

        // for project only via delegate
        project.CustomIdAlgorithm = project.HashedTargetPathIdAlgorithm;

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        Compiler.BuildWxs(project);
    }

    static public void AutoId_TargetPathHash_Custom()
    {
        var project =
             new Project("MyProduct",
                 new Dir(@"%ProgramFiles%\My Company\My Product",
                     new File(@"Files\Bin\MyApp.exe"),
                     new Dir(@"Docs\Manual",
                         new File(@"Files\Docs\Manual.txt"))));

        project.CustomIdAlgorithm = (WixEntity entity) =>
        {
            if (entity is File file)
            {
                var target_path = project.GetTargetPathOf(file);
                var hash = target_path.GetHashCode32();

                // WiX does not allow '-' char in ID. So need to use `Math.Abs`
                return $"{target_path.PathGetFileName()}_{Math.Abs(hash)}";
            }

            return null; // pass to default ID generator
        };

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        Compiler.BuildWxs(project);
    }
}