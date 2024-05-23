using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Resources;
using UnityEngine;
using Zeepkist.Lexer.Resources;
using ZeepSDK.Racing;
using ResourceManager = Zeepkist.Lexer.Resources.ResourceManager;

namespace Zeepkist.Lexer
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> SoundType { get; private set; }

        static New_ControlCar playerCar = null;
        static String currentSurface = null;

        private async void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            await ResourceManager.PreLoad();

            Plugin.SoundType = this.Config.Bind<string>("Mod", "Type", ResourceManager.GetTypes().First(), 
                new ConfigDescription("Sound type to play when driving on metal and ice",
                new AcceptableValueList<string>(ResourceManager.GetTypes())));

            Plugin.Enabled = this.Config.Bind<bool>("Mod", "Enabled", true);

            RacingApi.PlayerSpawned += RacingApi_PlayerSpawned;
        }

        private void RacingApi_PlayerSpawned()
        {
            playerCar = PlayerManager.Instance.currentMaster.carSetups.First().cc;
            Debug.Log($"Detected a player spawn, initializing...");
        }

        private void Update()
        {
            if (playerCar == null || Enabled.Value == false)
            {
                return;
            }

            if (playerCar.GetLocalVelocity().magnitude * 3.6f > 1)
            {
                var surfaces = playerCar.wheels.Where(x => x.isFrontWheel).Select(x => x.GetCurrentSurface().material.name).Distinct();

                if (surfaces.Count() == 1)
                {
                    if (currentSurface != surfaces.First())
                    {
                        Logger.LogInfo($"Detected surface change from {currentSurface} to {surfaces.First()}");
                        currentSurface = surfaces.First();

                        if (currentSurface.IndexOf("ice", StringComparison.OrdinalIgnoreCase) > -1) 
                        {
                            ResourceManager.Play(SoundType.Value, TypeEnum.Ice);
                        } else if (currentSurface.IndexOf("metal", StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            ResourceManager.Play(SoundType.Value, TypeEnum.Metal);
                        }
                    }
                }
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}