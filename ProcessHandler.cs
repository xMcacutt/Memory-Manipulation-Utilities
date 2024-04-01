namespace HellPie_Tools.Utility;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

internal class ProcessHandler
{
    public static bool MemoryWriteDebugLogging = false;
    public static bool MemoryReadDebugLogging = false;
    public static string MostRecentIOIndicator = "None";
    public static bool LogMostRecentMemoryIOInfoOnProcessExit = true;

    [DllImport("kernel32.dll")]
    private static extern unsafe bool ReadProcessMemory(
        nint hProcess,
        void* lpBaseAddress,
        void* lpBuffer,
        nuint nSize,
        nuint* lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess, 
        int lpBaseAddress, 
        byte[] lpBuffer, int 
            dwSize,
        out IntPtr lpNumberOfBytesWritten);
    
    public static bool WriteData(int address, byte[] bytes, string writeIndicator)
    {
        if (LogMostRecentMemoryIOInfoOnProcessExit) MostRecentIOIndicator = writeIndicator;
        return WriteData(address, bytes);
    }

    public static bool WriteData(int address, byte[] bytes)
    {
        try
        {
            return WriteProcessMemory(GameProcess.Handle, address, bytes, bytes.Length, out var bytesWritten);
        }
        catch (Exception ex)
        {
            //Logger.Write($"Error writing data: {ex}");
        }

        return false;
    }

    public static async Task<bool> WriteDataAsync(int address, byte[] bytes)
    {
        var t = Task.Run(() =>
        {
            Debug.WriteLine("ASYNC: Before return");
            return WriteProcessMemory(GameProcess.Handle, address, bytes, bytes.Length, out var bytesWritten);
        });
        Debug.WriteLine("ASYNC: Before try");
        try
        {
            Debug.WriteLine("ASYNC: Before await");
            await t;
        }
        catch (Exception ex)
        {
            //Logger.Write($"Error writing data: {ex}");
        }

        return t.Result;
    }


    public static bool TryRead<T>(nint address, out T result, bool addBase, string readIndicator) where T : unmanaged
    {
        if (LogMostRecentMemoryIOInfoOnProcessExit) MostRecentIOIndicator = readIndicator;
        return TryRead(address, out result, addBase);
    }

    private static unsafe bool TryRead<T>(nint address, out T result, bool addBase) where T : unmanaged
    {
        try
        {
            fixed (T* pResult = &result)
            {
                //string s = GetCallStackAsString();
                if (addBase) address = GameProcess.BaseAddress + address;
                nuint nSize = (nuint)sizeof(T), nRead;
                //BasicIoC.Logger.Write(address.ToString() + " " + s);
                return ReadProcessMemory(GameProcess.Handle, (void*)address, pResult, nSize, &nRead)
                       && nRead == nSize;
            }
        }
        catch (Exception ex)
        {
            //Logger.Write(ex.ToString());
            result = default;
            throw new GameProcessException("ProcessHandler.TryRead()", ex);
        }
    }

    public static unsafe bool TryReadBytes(nint address, out byte[] buffer, int length, bool addBase)
    {
        try
        {
            buffer = new byte[length];
            if (addBase) address = GameProcess.BaseAddress + address;

            fixed (byte* pBuffer = buffer)
            {
                nuint nRead;
                return ReadProcessMemory(GameProcess.Handle, (void*)address, pBuffer, (nuint)length, &nRead)
                       && nRead == (nuint)length;
            }
        }
        catch (Exception ex)
        {
            //Logger.Write(ex.ToString());
            throw new GameProcessException("ProcessHandler.TryRead()", ex);
        }
    }

    public static void CheckAddress<T>(int addr, T value, string indicator) where T : unmanaged
    {
        TryRead(addr, out T test, false, indicator);
        if (test.Equals(value)) return;
        //Logger.Write(indicator + " " + test);
    }
}