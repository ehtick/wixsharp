//css_dir ..\..\;
//css_ref Wix_bin\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref WixSharp.UI.dll;
//css_ref System.Core.dll;
//css_ref System.Xml.dll;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;
using io = System.IO;

public class Script
{
    static public void Main()
    {
        var binaries = new Feature("Binaries", "Product binaries", true, false);
        var docs = new Feature("Documentation", "Product documentation (manuals and user guides)", true);
        var tuts = new Feature("Tutorials", "Product tutorials", false);
        docs.Children.Add(tuts);

        var project =
            new ManagedProject("ManagedSetup",
                new Dir(@"%ProgramFiles%\My Company\My Product",
                    new File(binaries, @"Files\bin\MyApp.exe"),
                    new Dir("Docs",
                        new File(docs, "readme.txt"),
                        new File(tuts, "setup.cs"))));

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        // project.PreserveTempFiles = true;
        // project.ManagedUI = ManagedUI.Default;

        project.ManagedUI = new ManagedUI();

        project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
                                        // .Add<LicenceDialog>()
                                        // .Add<FeaturesDialog>()
                                        // .Add<InstallDirDialog>()
                                        .Add<ProgressDialog>()
                                        .Add<ExitDialog>();

        project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
                                       .Add<FeaturesDialog>()
                                       .Add<ProgressDialog>()
                                       .Add<ExitDialog>();

        project.UIInitialized += Project_UIInitialized;
        project.Load += msi_Load;
        project.AfterInstall += msi_AfterInstall;

        project.AddBinary(new Binary(new Id("en_wxl"), @"D:\dev\wixsharp4\Source\src\WixSharp.UI\ManagedUI\Images\WixUI_en-us.wxl"));

        project.BuildMsi();
    }

    static void Project_UIInitialized(SetupEventArgs e)
    {
        Debug.Assert(false);
        MsiRuntime runtime = e.ManagedUI.Shell.MsiRuntime();
        var langData = e.Session.ReadBinary("en_wxl");

        runtime.UIText.UpdateFromWxl(langData);

        // try
        // {
        //     e.Session["INSTALLDIR"] = Registry.CurrentUser
        //                                       .OpenSubKey(@"SOFTWARE\7-Zip", false)
        //                                       .GetValue("Path")
        //                                       .ToString();
        // }
        // catch
        // {
        // }

        // var message = e.Session["INSTALLDIR"];

        // MessageBox.Show(message.IsNotEmpty() ? message : "<not initialized yet>");
    }

    static void msi_Load(SetupEventArgs e)
    {
        if (e.IsInstalling)
        {
            //pseudo validation
            if (false && Environment.MachineName.Length > 3)
            {
                string message = "<<<<<<<<<< Your PC is too fancy for this app! >>>>>>>>>>";
                e.Session.Log(message);

                if (e.UILevel > 4)
                    MessageBox.Show(message, e.ProductName + " " + e.UILevel);

                e.Result = ActionResult.Failure;
            }
        }
    }

    static void msi_AfterInstall(SetupEventArgs e)
    {
        if (!e.IsUninstalling && e.UILevel >= 2)
        {
            string readme = io.Path.Combine(e.InstallDir, @"Docs\readme.txt");

            if (io.File.Exists(readme))
                Process.Start("notepad.exe", readme);
            else
                MessageBox.Show("Readme.txt is not present. You may want to download it from the product website.", e.ProductName);
        }
    }
}