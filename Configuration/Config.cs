using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace MoreScreams.Configuration
{
    internal static class Config
    {
        private const string CONFIG_FILE_NAME = "MoreScreams.cfg";

        private static ConfigFile config;
        private static ConfigEntry<float> shutUpAfter;

        public static void Init()
        {
            var filePath = Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME);
            config = new ConfigFile(filePath, true);
            shutUpAfter = config.Bind("Config", "Shut up after", 2f, "Mutes death player after given seconds.");
        }

        public static float ShutUpAfter => shutUpAfter.Value;
    }
}