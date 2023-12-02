﻿using Dissonance;
using GameNetcodeStuff;
using HarmonyLib;
using MoreScreams.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace MoreScreams.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    class UpdatePlayerVoiceEffectsPatch
    {
        private static bool updateStarted = false;
        private static Dictionary<PlayerControllerB, AudioConfig> configs = new Dictionary<PlayerControllerB, AudioConfig>();
        private static HashSet<PlayerControllerB> checkedPlayers = new HashSet<PlayerControllerB>();

        public static Dictionary<PlayerControllerB, AudioConfig> Configs { get => configs; }

        private static void Prefix()
        {
            if(configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();
            if (!updateStarted)
            {
                StartOfRound.Instance.StartCoroutine(UpdateNumerator());
                updateStarted = true;
            }

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
                checked
                {
                    if ((playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead) && !(playerControllerB == GameNetworkManager.Instance.localPlayerController))
                    {
                        AudioSource currentVoiceChatAudioSource = StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatAudioSource;
                        if (playerControllerB.isPlayerDead)
                        {
                            if (!configs.ContainsKey(playerControllerB) && !checkedPlayers.Contains(playerControllerB))
                            {
                                currentVoiceChatAudioSource.transform.position = playerControllerB.deadBody.transform.position;
                                checkedPlayers.Add(playerControllerB);
                                configs.Add(playerControllerB,
                                    new AudioConfig(
                                            Time.time + Config.ShutUpAfter,
                                            currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled,
                                            currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled,
                                            currentVoiceChatAudioSource.panStereo = 0f,
                                            SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)playerControllerB.playerClientId)],
                                            GetPitch(playerControllerB),
                                            currentVoiceChatAudioSource.spatialBlend,
                                            playerControllerB.currentVoiceChatIngameSettings.set2D,
                                            playerControllerB.voicePlayerState.Volume,
                                            playerControllerB.deadBody.transform,
                                            currentVoiceChatAudioSource.transform
                                        )
                                );
                            }
                        }
                    }
                }
            }
        }

        private static void Postfix()
        {
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            foreach (var playerControllerB in configs.Keys)
            {
                AudioConfig config = configs[playerControllerB];
                checked
                {
                    if ((playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead) && !(playerControllerB == GameNetworkManager.Instance.localPlayerController))
                    {
                        AudioSource currentVoiceChatAudioSource = playerControllerB.currentVoiceChatAudioSource;
                        if (playerControllerB.isPlayerDead)
                        {
                            currentVoiceChatAudioSource.transform.position = playerControllerB.deadBody.transform.position;
                            currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = config.LowPassFilter;
                            currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = config.HighPassFilter;
                            currentVoiceChatAudioSource.panStereo = config.PanStereo;
                            SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)playerControllerB.playerClientId)] = config.PlayerVoicePitchTargets;
                            SoundManager.Instance.SetPlayerPitch(config.PlayerPitch, unchecked((int)playerControllerB.playerClientId));

                            currentVoiceChatAudioSource.spatialBlend = config.SpatialBlend;
                            playerControllerB.currentVoiceChatIngameSettings.set2D = config.Set2D;
                            playerControllerB.voicePlayerState.Volume = config.Volume;
                        }
                    }
                }
            }
        }

        private static IEnumerator UpdateNumerator()
        {
            yield return 0;

            HashSet<PlayerControllerB> playerControllersToRemove = new HashSet<PlayerControllerB>();

            while (true)
            {
                CleanPlayers();
                yield return new WaitForEndOfFrame();
            }
        }
        private static void CleanPlayers()
        {

            if (configs == null)
                return;

            bool removed = false;

            foreach (KeyValuePair<PlayerControllerB, AudioConfig> pair in configs)
            {
                pair.Value.AudioSourceT.position = pair.Value.DeadBodyT.position;
            }

            foreach(var player in configs.Where(x => !x.Key.isPlayerDead || x.Value.ShutUpAt < Time.time).Select(x => x.Key).Union(checkedPlayers.Where(x => !x.isPlayerDead)))
            {
                removed = true;
                configs.Remove(player);
                if (!player.isPlayerDead) checkedPlayers.Remove(player);
            }


            if(removed) StartOfRound.Instance.UpdatePlayerVoiceEffects();

        }

        private static float GetPitch(PlayerControllerB playerControllerB)
        {
            checked
            {
                int playerObjNum = unchecked((int)playerControllerB.playerClientId);
                float pitch = 0f;
                SoundManager.Instance.diageticMixer.GetFloat($"PlayerPitch{playerObjNum}", out pitch);
                return pitch;
            }
        }
    }
}