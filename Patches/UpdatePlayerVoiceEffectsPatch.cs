using Dissonance;
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
                StartOfRound.Instance.StartCoroutine(FixedUpdateNumerator());
                updateStarted = true;
            }
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            RemoveAlivePlayers();

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

            RemoveAlivePlayers();

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

        private static IEnumerator FixedUpdateNumerator()
        {
            yield return 0;
            HashSet<PlayerControllerB> playerControllersToRemove = new HashSet<PlayerControllerB>();

            while (true)
            {
                if(configs == null)
                {
                    yield return null;
                    continue;
                }

                playerControllersToRemove.Clear();
                foreach (var conf in configs)
                {
                    if(conf.Value.ShutUpAt < Time.time)
                    {
                        playerControllersToRemove.Add(conf.Key);
                    }
                    conf.Value.AudioSourceT.position = conf.Value.DeadBodyT.position;
                }

                foreach (var key in playerControllersToRemove)
                {
                    configs.Remove(key);
                }

                yield return new WaitForFixedUpdate();
            }
        }
        private static void RemoveAlivePlayers()
        {
            List<PlayerControllerB> toRemove = new List<PlayerControllerB>();
            foreach (KeyValuePair<PlayerControllerB, AudioConfig> pair in configs)
            {
                if (!pair.Key.isPlayerDead)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (PlayerControllerB player in checkedPlayers)
            {
                if (!player.isPlayerDead)
                {
                    toRemove.Add(player);
                }
            }

            foreach (var key in toRemove)
            {
                configs.Remove(key);
                checkedPlayers.Remove(key);
            }
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