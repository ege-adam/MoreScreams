using Dissonance;
using Dissonance.Audio.Playback;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MoreScreams.Patches
{
    [HarmonyPatch]
    class DissonancePatch
    {
        public static MethodBase TargetMethod()
        {
            // use normal reflection or helper methods in <AccessTools> to find the method/constructor
            // you want to patch and return its MethodInfo/ConstructorInfo
            //
            return AccessTools.FirstMethod(typeof(VoicePlayback), method => method.Name.Contains("SetTransform"));
        }

        static bool Prefix(object __instance)
        {
            foreach(AudioConfig conf in UpdatePlayerVoiceEffectsPatch.Configs.Values)
            {
                if((__instance as VoicePlayback).transform.Equals(conf.AudioSourceT))
                {
                    return false;
                }
            }

            return true;
        }


        static void UpdateAudioSourcePoses()
        {
            foreach (AudioConfig conf in UpdatePlayerVoiceEffectsPatch.Configs.Values)
            {
                conf.AudioSourceT.position = conf.DeadBodyT.position;
            }
        }
    }
}