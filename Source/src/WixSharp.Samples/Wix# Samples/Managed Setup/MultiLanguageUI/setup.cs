//css_dir ..\..\..\;
//css_ref Wix_bin\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref WixSharp.UI.dll;
//css_ref System.Core.dll;
//css_ref System.Xml.dll;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Forms;
using WixSharp.UI.Forms;

public static class Script
{
    static public void Main(string[] args)
    {
        var project = new ManagedProject("ManagedSetup",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File(@"..\Files\bin\MyApp.exe"),
                                  new Dir("Docs",
                                      new File("readme.txt"),
                                      new File(@"..\Files\Docs\tutorial.txt"))));

        project.ManagedUI = ManagedUI.Default;
        project.ManagedUI.Icon = "app.ico";
        project.MinimalCustomDrawing = true;

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        project.ControlPanelInfo.InstallLocation = "[INSTALLDIR]";

        project.SetNetFxPrerequisite(Condition.Net45_Installed, "Please install .Net 4.5 First");

        // project.PreserveTempFiles = true;

        project.Localize();

        project.BuildMsi();
    }

    static SupportedLanguages DetectLanguage()
    {
        // In production you can do something smarter like analyzing OS language.
        // Current thread UI culture most likely will not work as it will be set to the language of the project
        // Consider using WixSharp.Native.GetPreferredIsoTwoLetterUILanguages to get OS preferred languages info

        string[] OS_PreferredLanguages = Native.GetPreferredIsoTwoLetterUILanguages();

        var input = new Form
        {
            Size = new Size(140, 60),
            Text = "Language Selection",
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            ShowIcon = false,
            TopMost = true,
            StartPosition = FormStartPosition.CenterScreen
        };

        var langSelection = new ComboBox { Dock = DockStyle.Fill };
        langSelection.Items.Add("English");
        langSelection.Items.Add("German");
        langSelection.Items.Add("Greek");
        langSelection.SelectedIndex = 0;
        langSelection.SelectedIndexChanged += (s, e) => input.Close();

        input.Controls.Add(langSelection);

        input.ShowDialog();

        return (SupportedLanguages)langSelection.SelectedIndex;
    }

    static void Localize(this ManagedProject project)
    {
        project.AddBinary(new Binary(new Id("de_xsl"), "WixUI_de-DE.wxl"))
               .AddBinary(new Binary(new Id("gr_xsl"), "WixUI_el-GR.wxl"));

        project.UIInitialized += (SetupEventArgs e) =>
        {
            // Debug.Assert(false);

            MsiRuntime runtime = e.ManagedUI.Shell.MsiRuntime();

            switch (DetectLanguage())
            {
                case SupportedLanguages.German:
                    runtime.UIText.UpdateFromWxl(e.Session.ReadBinary("de_xsl"));
                    break;

                case SupportedLanguages.Greek:
                    runtime.UIText.UpdateFromWxl(e.Session.ReadBinary("gr_xsl"));

                    break;
            }
        };

        project.UILoaded += (SetupEventArgs e) =>
        {
            // first dialog is loaded
            MsiRuntime runtime = e.ManagedUI.Shell.MsiRuntime();
            runtime.UIText.UpdateFromWxl(e.Session.ReadBinary("WixSharp_UIText")); // translate back to English

            e.ManagedUI.OnCurrentDialogChanged += (IManagedDialog obj) =>
            {
                // any dialog after the first one is loaded
            };
        };
    }
}

public enum SupportedLanguages
{
    English,
    German,
    Greek
}