namespace HellPie_Tools.Utility;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal class GameProcess
{
    private static Process process;
    private static bool handleOpen;
    private const string PROCESS_NAME = "Hell Pie";

    public static bool IsRunning { get; private set; }
    public static bool LaunchingGame { get; private set; }
    public static bool CanLaunchGame => !IsRunning && !LaunchingGame;

    public static nint BaseAddress { get; private set; }

    public static IntPtr Handle { get; private set; }

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr handle);

    public static event Action OnGameProcessExited = delegate { };
    public static event Action OnGameProcessFound = delegate { };
    public static event Action OnGameProcessLaunched = delegate { };
    public static event Action OnGameProcessLaunchFailed = delegate { };

    //Launches the game from the path saved in settings.
    //Returns whether or not game was succesfully launched.
    public static bool TryLaunchGame()
    {
        //If game is already running, or being launched, fail
        if (!CanLaunchGame)
            return false;
        //If we dont have a valid path to the exe, fail
        if (!SettingsHandler.HasValidExePath())
            return false;

        LaunchingGame = true;
        try
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo(SettingsHandler.Settings.GameFolderPath, "-noidle")
                { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(SettingsHandler.Settings.GameFolderPath);
            process.Start();
            PullProcessData();
            OnGameProcessLaunched?.Invoke();
            LaunchingGame = false;
            return true;
        }
        catch
        {
            OnGameProcessLaunchFailed?.Invoke();
            LaunchingGame = false;
            return false;
        }
    }

    public static void CloseProcess()
    {
        try
        {
            process?.CloseMainWindow();
        }
        catch
        {
            
        }
    }

    //Returns true if process is running
    //Attempts to find the process if not, returns true if successfully found
    //Returns false if process is closed or is being launched
    public static bool FindProcess()
    {
        if (IsRunning)
            return true;
        if (LaunchingGame)
            return false;

        var processes = Process.GetProcessesByName(PROCESS_NAME);

        if (processes.Length == 0)
            return false;
        if (processes.Length > 1)
        {
            //Use first non-exiting process
            var setProcess = false;
            for (var i = 1; i < processes.Length; i++)
                if (processes[i].HasExited)
                {
                    processes[i].Dispose();
                    processes[i].Close();
                }
                else if (!setProcess)
                {
                    process = processes[i];
                    OnGameProcessFound?.Invoke();
                    PullProcessData();
                    setProcess = true;
                }

            return setProcess;
        }

        process = processes[0];
        OnGameProcessFound?.Invoke();
        PullProcessData();
        return true;
    }

    private static void PullProcessData()
    {
        process.EnableRaisingEvents = true;
        process.Exited += OnExit;
        Handle = OpenProcess(0x1F0FFF, false, process.Id);
        handleOpen = true;

        //Takes a little while to open from process.Start(), so we wait until we can find the mainmodule before continuing
        while (process.MainModule == null)
        {
        }

        BaseAddress = process.MainModule.BaseAddress;
        SettingsHandler.Settings.GameFolderPath = process.MainModule.FileName;
        SettingsHandler.Save();
        IsRunning = true;
    }

    private static void OnExit(object o, EventArgs e)
    {
        //Logger.WriteDebug($"Process exited: {process.ExitCode}");
        //if (ProcessHandler.LogMostRecentMemoryIOInfoOnProcessExit)
            //Logger.WriteDebug($"Last Memory IO operation: {ProcessHandler.MostRecentIOIndicator}");
        OnGameProcessExited?.Invoke();
        IsRunning = false;
        process.Close();
        CloseHandle();
        process.Dispose();
        process.Refresh();
        if (SettingsHandler.Settings.AutoRestartGameOnCrash) TryLaunchGame();
    }

    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        //Logger.WriteDebug($"Output data received: {e.Data}");
    }

    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        //Logger.WriteDebug($"Error data received: {e.Data}");
    }

    public static void CloseHandle()
    {
        if (!handleOpen)
            return;

        var successfullyClosed = CloseHandle(Handle);
        handleOpen = false;

        //if (!successfullyClosed) 
            //Logger.WriteDebug("Handle was not successfully closed!");
        //oh well
    }
}