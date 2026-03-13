using Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace EverySceneUtil {
    /// <summary>
    /// A developer tool for loading every scene in the game
    /// </summary>
    public class EverySceneUtil: Mod {
        /// <summary>
        /// GetName
        /// </summary>
        /// <returns></returns>
        new public string GetName() => "EverySceneUtil";
        /// <summary>
        /// GetVersion
        /// </summary>
        /// <returns></returns>
        public override string GetVersion() => "1.0.0.0";

        private static bool isLoading = false;
        private static KeyCode killswitch;

        /// <inheritdoc/>
        public override void Initialize() {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string filename = assembly.GetManifestResourceNames().Single(str => str.EndsWith("transitions.json"));
            using Stream jsonStream = assembly.GetManifestResourceStream(filename);
            foreach(TransitionData data in new ParseJson(jsonStream).parseFile<TransitionData>())
                TransitionData.allTransitions.Add(data);

            On.GameManager.OnNextLevelReady += OnLevelReady;
        }

        private void OnLevelReady(On.GameManager.orig_OnNextLevelReady orig, GameManager self) {
            orig(self);
            isLoading = false;
        }

        /// <summary>
        /// Loads every scene in the vanilla game sequentially and invokes provided actions on each one
        /// </summary>
        /// <param name="parameters">Customize how boss scenes and infection are handled</param>
        /// <param name="killswitch">Hold this key to halt the process immediately</param>
        /// <returns></returns>
        public static async Task ForEveryScene(ESU_Params parameters, KeyCode killswitch) {
            EverySceneUtil.killswitch = killswitch;
            PlayerData pd = PlayerData.instance;
            try {
                foreach(TransitionData data in TransitionData.allTransitions) {
                    await _esu_Infection(data, parameters, pd, true);
                }
            }
            catch(InterruptEveryScene) {
                Modding.Logger.Log($"[EverySceneUtil] - Interrupted loading scenes via manual killswitch");
            }
        }

        /// <summary>
        /// Loads a custom list of scenes sequentially and invokes provided actions on each one. Intended to be used to load non-vanilla scenes.
        /// </summary>
        /// <param name="sceneTransitions">The scene name and the transition from which to enter</param>
        /// <param name="parameters">Provide BeforeLoad and OnLoad actions (additional scenes and infection are mostly unused)</param>
        /// <returns></returns>
        public static async Task ForSpecificScenes((string scene, string gate)[] sceneTransitions, ESU_Params parameters) {
            foreach((string scene, string gate) in sceneTransitions) {
                await ProcessScene(scene, gate, parameters, "", false);
            }
        }

        private static async Task _esu_Infection(TransitionData data, ESU_Params parameters, PlayerData pd, bool hasKillswitch) {
            bool storedInfection = pd.crossroadsInfected;
            try {
                if(data.infection) {
                    switch(parameters.Infection) {
                        case InfectionOptions.NeverInfected:
                            pd.crossroadsInfected = false;
                            await _esu_Tests(data, parameters, pd, ", crossroadsInfected = false", hasKillswitch);
                            break;
                        case InfectionOptions.AlwaysInfected:
                            pd.crossroadsInfected = true;
                            await _esu_Tests(data, parameters, pd, ", crossroadsInfected = true", hasKillswitch);
                            break;
                        case InfectionOptions.Both:
                            pd.crossroadsInfected = false;
                            await _esu_Tests(data, parameters, pd, ", crossroadsInfected = false", hasKillswitch);
                            pd.crossroadsInfected = true;
                            await _esu_Tests(data, parameters, pd, ", crossroadsInfected = true", hasKillswitch);
                            break;
                        case InfectionOptions.Ignore:
                        default:
                            await _esu_Tests(data, parameters, pd, "", hasKillswitch);
                            break;
                    }
                }
                else {
                    await _esu_Tests(data, parameters, pd, "", hasKillswitch);
                }
            }
            catch(InterruptEveryScene) {
                pd.crossroadsInfected = storedInfection;
                throw;
            }
            finally {
                pd.crossroadsInfected = storedInfection;
            }
        }

        private static async Task _esu_Tests(TransitionData data, ESU_Params parameters, PlayerData pd, string additionalMessage, bool hasKillswitch) {
            Dictionary<string, bool> storedBools = new();
            Dictionary<string, int> storedInts = new();
            foreach(BoolTest test in data.boolTests) {
                storedBools.Add(test.name, pd.GetBool(test.name));
            }
            foreach(IntTest test in data.intTests) {
                storedInts.Add(test.name, pd.GetInt(test.name));
            }
            try {
                if(parameters.AdditionalScenes == AdditionalSceneOptions.AlwaysLoadWithExtras || parameters.AdditionalScenes == AdditionalSceneOptions.WithAndWithoutExtras) {
                    string newAddMsg = "";
                    foreach(BoolTest test in data.boolTests) {
                        pd.SetBool(test.name, test.value);
                        newAddMsg += $", {test.name} = {test.value}";
                    }
                    foreach(IntTest test in data.intTests) {
                        pd.SetInt(test.name, test.value);
                        newAddMsg += $", {test.name} = {test.value}";
                    }
                    await ProcessScene(data.scene, data.gate, parameters, additionalMessage + newAddMsg, hasKillswitch);
                }
                if(parameters.AdditionalScenes == AdditionalSceneOptions.WithAndWithoutExtras || data.hasAlt) {
                    string newAddMsg = "";
                    foreach(BoolTest test in data.boolTests) {
                        pd.SetBool(test.name, !test.value);
                        newAddMsg += $", {test.name} = {!test.value}";
                    }
                    foreach(IntTest test in data.intTests) {
                        pd.SetInt(test.name, 0);
                        newAddMsg += $", {test.name} = 0";
                    }
                    await ProcessScene(data.scene, data.gate, parameters, additionalMessage + newAddMsg, hasKillswitch);
                }
                if(parameters.AdditionalScenes == AdditionalSceneOptions.Ignore) {
                    await ProcessScene(data.scene, data.gate, parameters, additionalMessage, hasKillswitch);
                }
            }
            catch(InterruptEveryScene) {
                foreach(string key in storedBools.Keys) {
                    pd.SetBool(key, storedBools[key]);
                }
                foreach(string key in storedInts.Keys) {
                    pd.SetInt(key, storedInts[key]);
                }
                throw;
            }
            finally {
                foreach(string key in storedBools.Keys) {
                    pd.SetBool(key, storedBools[key]);
                }
                foreach(string key in storedInts.Keys) {
                    pd.SetInt(key, storedInts[key]);
                }
            }
        }

        private static async Task ProcessScene(string scene, string gate, ESU_Params parameters, string additionalMessage, bool hasKillswitch) {
            if(hasKillswitch && Input.GetKey(killswitch))
                throw new InterruptEveryScene();
            if(parameters.LogSceneName)
                Modding.Logger.Log($"[EverySceneUtil] - ===== Loading {scene}{additionalMessage} =====");
            parameters.BeforeLoad?.Invoke(scene);
            isLoading = true;
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo {
                SceneName = scene,
                EntryGateName = gate,
                HeroLeaveDirection = GlobalEnums.GatePosition.unknown,
                EntryDelay = 0,
                WaitForSceneTransitionCameraFade = true,
                PreventCameraFadeOut = false,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                AlwaysUnloadUnusedAssets = false,
                forceWaitFetch = false
            });
            while(isLoading)
                await Task.Yield();
            do {
                await Task.Yield();
            } while(HeroController.instance.cState.transitioning);
            parameters.OnLoad?.Invoke();
        }
    }

    internal class InterruptEveryScene: Exception;

    /// <summary>
    /// Parameters used when calling ForEveryScene() or ForSpecificScenes()
    /// </summary>
    public struct ESU_Params {
        /// <summary>
        /// Required constructor
        /// </summary>
        public ESU_Params() {}
        /// <summary>
        /// Called before loading the next scene, passes the scene name as a string
        /// </summary>
        public Action<string> BeforeLoad = null;
        /// <summary>
        /// Called once the scene has fully loaded
        /// </summary>
        public Action OnLoad = null;
        /// <summary>
        /// Writes the scene name and any relevant load parameters to modlog.txt
        /// </summary>
        public bool LogSceneName = true;
        /// <summary>
        /// Determines how secondary boss scenes are handled
        /// </summary>
        public AdditionalSceneOptions AdditionalScenes = AdditionalSceneOptions.Ignore;
        /// <summary>
        /// Determines how infected crossroads is handled
        /// </summary>
        public InfectionOptions Infection = InfectionOptions.Ignore;
    }

    /// <summary>
    /// Determines how secondary boss scenes are handled
    /// </summary>
    public enum AdditionalSceneOptions {
        /// <summary>
        /// Do not acknowledge boss scenes, load the scene once as-is
        /// </summary>
        Ignore,
        /// <summary>
        /// Load the scene once with additional scene active, only repeat if an alt scene exists
        /// </summary>
        AlwaysLoadWithExtras,
        /// <summary>
        /// Load the scene with no boss scene and with every version of the boss scene
        /// </summary>
        WithAndWithoutExtras
    }

    /// <summary>
    /// Determines how infected crossroads is handled
    /// </summary>
    public enum InfectionOptions {
        /// <summary>
        /// Do not acknowledge infection, load the scene once as-is
        /// </summary>
        Ignore,
        /// <summary>
        /// Disable infection before loading scenes
        /// </summary>
        NeverInfected,
        /// <summary>
        /// Enable infection before loading scenes
        /// </summary>
        AlwaysInfected,
        /// <summary>
        /// Load both infected and uninfected variations of the scene
        /// </summary>
        Both
    }
}