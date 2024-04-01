using System.IO;
using Newtonsoft.Json;

namespace HellPie_Tools.Utility;

internal static class SettingsHandler
{
    public static Settings Settings { get; private set; }

    public static void Setup()
    {
        //MAIN SETTINGS
        var json = File.ReadAllText("./Settings.json");
        Settings = JsonConvert.DeserializeObject<Settings>(json);
    }

    public static void Save()
    {
        var json = JsonConvert.SerializeObject(Settings);
        File.WriteAllText("./Settings.json", json);
    }

    public static bool HasValidExePath()
    {
        return Settings.GameFolderPath != "" && Directory.Exists(Settings.GameFolderPath);
    }
}