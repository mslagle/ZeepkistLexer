using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Zeepkist.Lexer.Resources
{
    public enum TypeEnum
    {
        Ice,
        Metal
    }

    public static class ResourceManager
    {
        static Dictionary<string, SoundResource> SoundResources = new Dictionary<string, SoundResource>();
        static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ResourceManager");

        public static string[] GetTypes()
        {
            return SoundResources.Keys.ToArray();
        }

        public static void Play(string typeName, TypeEnum type)
        {
            Logger.LogInfo($"Playing audio clip {typeName} of type {type}");
            
            try
            {
                var soundResource = SoundResources[typeName];
                AudioClip soundClip;

                if (type == TypeEnum.Ice)
                {
                    soundClip = soundResource.IceAudioClip;
                } else if (type == TypeEnum.Metal)
                {
                    soundClip = soundResource.MetalAudioClip;
                } else
                {
                    return;
                }

                AudioManager audioManager = GetOrCreateAudioManager();
                audioManager.Play(new AudioItemScriptableObject() { Clip = soundClip, BaseVolume = 1f, Loop = false });

                Logger.LogInfo($"Successfully played audio clip of {soundClip.name}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public static AudioManager GetOrCreateAudioManager()
        {
            if (AudioManager.Instance == null)
            {
                Logger.LogInfo($"AudioManager is null, creating a new instance");

                AudioManager audioManager = new AudioManager();
                AudioManager.Instance = audioManager;
            }

            return AudioManager.Instance;
        }

        public static async Task PreLoad()
        {
            string dllFile = System.Reflection.Assembly.GetAssembly(typeof(ResourceManager)).Location;
            string dllDirectory = Path.GetDirectoryName(dllFile);

            Logger.LogInfo($"Preloading sound resources at {dllDirectory}");
            var matchingFiles = Directory.GetFiles(dllDirectory).Where(x => Path.GetExtension(x) == ".mp3");

            var names = matchingFiles.Select(x => Path.GetFileNameWithoutExtension(x).Split(".").First()).Distinct();
            foreach (var name in names)
            {
                var iceFile = Path.Combine(dllDirectory, $"{name}.ice.mp3");
                var metalFile = Path.Combine(dllDirectory, $"{name}.metal.mp3");

                if (!File.Exists(iceFile))
                {
                    Logger.LogWarning($"The file {iceFile} does not exist.  Not creating a resource of type {name}");
                    continue;
                }

                if (!File.Exists(metalFile))
                {
                    Logger.LogWarning($"The file {metalFile} does not exist.  Not creating a resource of type {name}");
                    continue;
                }

                Logger.LogInfo($"Loading resources for {name} with {iceFile} and {metalFile}");

                try
                {
                    AudioClip iceAudioClip = await GetAudioClip(iceFile);
                    AudioClip metalAudioClip = await GetAudioClip(metalFile);

                    SoundResources.Add(name, new SoundResource(name, iceAudioClip, metalAudioClip));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        public static async Task<AudioClip> GetAudioClip(string path)
        {
            Logger.LogInfo($"Creating an audio clip from path {path}");

            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
            {
                uwr.SendWebRequest();

                // wrap tasks in try/catch, otherwise it'll fail silently
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);

                    if (uwr.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Logger.LogError($"{uwr.error}");
                    }
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogInfo($"{err.Message}, {err.StackTrace}");
                }
            }

            if ( clip != null )
            {
                Logger.LogInfo($"Successfully created an audio clip from path {path}");
            }
            return clip;
        }
    }

    public class SoundResource
    {
        public string SoundType { get; set; }
        public AudioClip IceAudioClip { get; set; }
        public AudioClip MetalAudioClip { get; set; }

        public SoundResource(string soundType, AudioClip iceAudioClip, AudioClip metalAudioClip)
        {
            SoundType = soundType;
            IceAudioClip = iceAudioClip;
            MetalAudioClip = metalAudioClip;
        }
    }


}
