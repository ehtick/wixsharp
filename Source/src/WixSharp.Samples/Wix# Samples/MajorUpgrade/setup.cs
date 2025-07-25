//css_dir ..\..\;

//css_ref Wix_bin\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref System.Core.dll;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;

class Script
{
    static public void Main(string[] args)
    {
        ManagedUIAproach();
        // NativeUIApproach();
        // ManagedUICustomCheckAproach();
    }

    static ManagedProject CreateProject(string version = "1.0.209.10040")
    {
        var project =
            new ManagedProject("TestProduct",
                new Dir(@"%ProgramFiles%\My Company\My Product",
                    new File(@"Files\1\MyApp.exe"),
                    new File(@"Files\1\MyApp.cs"),
                    new File(@"Files\1\readme.txt")));

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");
        project.Version = new Version(version);

        project.MajorUpgrade = new MajorUpgrade
        {
            Schedule = UpgradeSchedule.afterInstallInitialize,
            DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
        };

        project.EnableUpgradingFromPerUserToPerMachine();

        project.Load +=
            e => MessageBox.Show(e.Session.GetMainWindow(), e.ToString(), "Before (Install/Uninstall) - " + e.Session.QueryProductVersion());

        // project.PreserveTempFiles = true;

        return project;
    }

    static void NativeUIApproach()
    {
        for (int i = 0; i < 2; i++)
        {
            ManagedProject project = CreateProject($"1.{i}.209.10040");
            Compiler.BuildMsi(project, $"setup.v{project.Version}.msi");
        }
    }

    static public void ManagedUIAproach()
    {
        MSBuild.EmitAutoGenFiles = true;

        for (int i = 0; i < 2; i++)
        {
            ManagedProject project = CreateProject($"1.{i}.209.10040");

            project.ManagedUI = ManagedUI.Default;
            project.MajorUpgrade = MajorUpgrade.Default;

            Compiler.BuildMsi(project, $"setup.v{project.Version}.msi");
        }
    }

    static public void ManagedUICustomCheckAproach()
    {
        for (int i = 0; i < 2; i++)
        {
            ManagedProject project = CreateProject($"1.{i}.209.10040");

            // Note the `project.UIInitialized += ...` code below is for demo purpose only. It demonstrates custom handling
            // of downgrade condition.

            project.ManagedUI = ManagedUI.Default;
            project.UIInitialized += (SetupEventArgs e) =>
            {
                Version installedVersion = e.Session.LookupInstalledVersion();
                Version thisVersion = e.Session.QueryProductVersion();

                if (thisVersion <= installedVersion)
                {
                    MessageBox.Show("Later version of the product is already installed : " + installedVersion);

                    e.ManagedUI.Shell.ErrorDetected = true;

                    // provide custom error description if required
                    // e.ManagedUI.Shell.CustomErrorDescription = "Setup was aborted, because Later version of the product is already installed.";

                    e.Result = ActionResult.UserExit;
                }
            };

            Compiler.BuildMsi(project, $"setup.v{project.Version}.msi");
        }
    }
}