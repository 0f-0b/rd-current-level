using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RDLevelEditor;

namespace RDCurrentLevel
{
  [BepInPlugin("ud2.rdcurrentlevel", "RDCurrentLevel", "0.1.0")]
  [BepInProcess("Rhythm Doctor.exe")]
  public class RDCurrentLevelPlugin : BaseUnityPlugin
  {
    private static ConfigEntry<string>[] configFormats;

    public void Awake()
    {
      configFormats = new[] {
        Config.Bind("General", "Format0", "[artist] - [song]"),
        Config.Bind("General", "Format1", "[author]"),
        Config.Bind("General", "Format2", ""),
        Config.Bind("General", "Format3", ""),
        Config.Bind("General", "Format4", ""),
        Config.Bind("General", "Format5", ""),
        Config.Bind("General", "Format6", ""),
        Config.Bind("General", "Format7", ""),
        Config.Bind("General", "Format8", ""),
        Config.Bind("General", "Format9", ""),
      };
      _ = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(RDStartup), nameof(RDStartup.Setup))]
    public class ClearFilesOnStartup
    {
      public static void Postfix()
      {
        ClearFiles();
      }
    }

    [HarmonyPatch(typeof(scnGame), nameof(scnGame.EndLevel))]
    public class ClearFilesWhenLevelEnds
    {
      public static void Postfix()
      {
        ClearFiles();
      }
    }

    [HarmonyPatch(typeof(scnGame), "LoadingRoutine")]
    public class WriteFilesWhenLevelLoads
    {
      public static void Postfix()
      {
        var data = RDLevelData.current;
        if (data is null || scnGame.levelToLoadSource != LevelSource.ExternalPath)
          ClearFiles();
        else
          WriteFiles(data.settings);
      }
    }

    private static readonly Regex replacement = new(@"\[\w+\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static void WriteFiles(RDLevelSettings settings)
    {
      var song = RemoveCR(settings.song);
      var artist = RemoveCR(settings.artist);
      var author = RemoveCR(settings.author);
      foreach (var entry in configFormats.Select((x, i) => new { x.Value, Index = i }))
      {
        var text = replacement.Replace(entry.Value, delegate (Match match)
        {
          return match.Value switch
          {
            "[song]" => song,
            "[artist]" => artist,
            "[author]" => author,
            _ => match.Value,
          };
        });
        File.WriteAllText(GetPath(entry.Index), text);
      }
    }

    private static void ClearFiles()
    {
      foreach (var index in configFormats.Select((x, i) => i))
        File.WriteAllText(GetPath(index), "");
    }

    private static string GetPath(int index)
    {
      var root = Path.Combine(Directory.GetParent(Persistence.GetSavePath()).FullName, "CurrentLevel");
      if (!Directory.Exists(root))
        Directory.CreateDirectory(root);
      return Path.Combine(root, $"{index}.txt");
    }

    private static string RemoveCR(string s)
    {
      return s.Replace("\r", "");
    }
  }
}
