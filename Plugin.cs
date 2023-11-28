using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MoreScreams.Configuration;
using System.Reflection;
using System;

namespace MoreScreams
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class MoreScreams : BaseUnityPlugin
    {
        private const string modGUID = "egeadam.MoreScreams";
        private const string modName = "MoreScreams";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static MoreScreams Instance;


        internal static ManualLogSource mls;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            Configuration.Config.Init();
            harmony.PatchAll();

        }
    }


}