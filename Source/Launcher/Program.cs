using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using Microsoft.Win32;

namespace Launcher;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        new Application();
        BuildAvaloniaApp()
            .UsePlatformDetect()
            .SetupWithoutStarting();

        // Check for any missing components.
        var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        List<DependencyHint> missingDependencies = new List<DependencyHint>();

        CheckNetFx(missingDependencies);

        if (missingDependencies.Count > 0)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var item in missingDependencies)
                builder.AppendLine(item.Name);

            if (ShowDialog("Open Rails requires the following:\n\n{builder.ToString()}" +
                "\nWhen you click OK, we will guide you to download the required software.\n" +
                (missingDependencies.Count > 1 ? "If there are multiple items missing, you need to repeat this process until all dependencies are resolved.\n" : string.Empty) +
                "Click Cancel to quit.",
                    "Open Rails", ButtonEnum.OkCancel, Icon.Error) == ButtonResult.Ok)
            {
                DownloadDependency(missingDependencies[0]);
            }
            return;
        }

        List<string> missingORFiles = new List<string>();
        CheckOR(missingORFiles, path);
        if (missingORFiles.Count > 0)
        {
            ShowDialog("Open Rails",$"Open Rails is missing the following:\n\n{string.Join("\n", missingORFiles.ToArray())}\n\nPlease re-install the software.",  ButtonEnum.Ok, Icon.Error);
            return;
        }
        // Default menu
        var process = Process.Start(Path.Combine(path, "Menu.exe"));
        process.WaitForInputIdle();
    }

    internal struct DependencyHint
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string Text { get; set; }
    }

    private static void DownloadDependency(DependencyHint dependency)
    {
        ShowDialog("Open Rails", $"{dependency.Text} \n\nWhen you click OK, we will try to open a browser window pointing to the URL. ", ButtonEnum.Ok, Icon.Info);
        Process.Start(dependency.Url);
    }

    static void CheckNetFx(List<DependencyHint> missingDependencies)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        
        using (var RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            if ((SafeReadKey(RK, "Install", 0) == 1) && (SafeReadKey(RK, "Release", 0) >= 461808))  //https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#find-net-framework-versions-45-and-later-with-code
                return;

        missingDependencies.Add(new DependencyHint()
        {
            Name = ("Microsoft .NET Framework 4.7.2 or later"),
            Text = "Please go to\n https://support.microsoft.com/en-us/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows \nto download the installation package " +
            "for Microsoft .NET Framework 4.7.2 and install the software.",
            Url = "https://support.microsoft.com/en-us/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows"
        });
    }

    static void CheckOR(List<string> missingFiles, string path)
    {
        foreach (var file in new[] {
            // Required libraries:
            "GNU.Gettext.dll",
            "GNU.Gettext.WinForms.dll",
            "ICSharpCode.SharpZipLib.dll",
            "DotNetZip.dll",
            @"Native/X86/OpenAL32.dll",
            @"Native/X64/OpenAL32.dll",
            // Programs:
            "Menu.exe",
            "RunActivity.exe",
        })
        {
            if (!File.Exists(Path.Combine(path, file)))
                missingFiles.Add($"File '{file}'");
        }
    }

    static int SafeReadKey(RegistryKey key, string name, int defaultValue)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new NotSupportedException("Windows Registry only available under Microsoft Windows!");
        }

        try
        {
            return (int)key.GetValue(name, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static ButtonResult ShowDialog(string title, string message, ButtonEnum buttonEnum, Icon icon)
    {
        CancellationTokenSource source = new CancellationTokenSource();
        Task<ButtonResult> dialogTask = MessageBox.Avalonia.MessageBoxManager
            .GetMessageBoxStandardWindow(title, message, buttonEnum, icon).Show();
        Task.Run(() => Dispatcher.UIThread.MainLoop(source.Token), source.Token);
        return dialogTask.Result;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
