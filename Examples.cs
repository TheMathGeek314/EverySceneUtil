using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EverySceneUtil {
    internal class Examples {

        //Log the locations of every Stalactite in the game
        internal static async void LocateStalactites() {
            List<(string, string, float, float)> spikes = new();
            ESU_Params p = new() {
                OnLoad = logSpikes,
                AdditionalScenes = AdditionalSceneOptions.Ignore,
                Infection = InfectionOptions.NeverInfected,
                LogSceneName = false
            };
            await EverySceneUtil.ForEveryScene(p, KeyCode.P);
            foreach(var spike in spikes) {
                Modding.Logger.Log($"{spike.Item1}\t{spike.Item2}\t{spike.Item3}\t{spike.Item4}");
            }

            void logSpikes() {
                foreach(GameObject sc in GameObject.FindObjectsOfType<StalactiteControl>().Select(sc => sc.gameObject)) {
                    var info = (sc.scene.name, sc.name, sc.transform.position.x, sc.transform.position.y);
                    if(!spikes.Contains(info))
                        spikes.Add(info);
                }
            }
        }

        
        //Load the first few rooms of Summit from the Knight of Nights plando
        //  (rooms must exist, in this case you must be in the plando save file)
        internal static async void LoadKnightOfNightsRooms() {
            ESU_Params p = new() {
                LogSceneName = true
            };
            (string, string)[] summitGates = [
                ("Summit_EntryHall", "bot1"),
                ("Summit_EntryPlain", "bot1"),
                ("Summit_Tunnels", "left1"),
                ("Summit_WindCliffs", "left1"),
                ("Summit_SpikeTunnels", "bot1")
            ];
            await EverySceneUtil.ForSpecificScenes(summitGates, p);
        }


        //Loads each golf course from MilliGolf
        //  (obviously requires MilliGolf to be installed)
        internal static async void LoadGolfCourses() {
            (string, string)[] courses = [
                ("Town", "left1"),
                ("Crossroads_07", "right1"),
                ("RestingGrounds_05", "left2"),
                ("Hive_03", "right1"),
                ("Fungus1_31", "right1"),
                ("Fungus3_02", "left1"),
                ("Deepnest_East_11", "right1"),
                ("Waterways_02", "top1"),
                ("Cliffs_01", "right1"),
                ("Abyss_06_Core", "top1"),
                ("Fungus2_12", "left1"),
                ("Ruins1_30", "left2"),
                ("Abyss_04", "top1"),
                ("Fungus3_04", "left1"),
                ("Ruins1_03", "right1"),
                ("Deepnest_35", "top1"),
                ("Mines_23", "right1"),
                ("White_Palace_19", "top1")
            ];
            ESU_Params p = new() {
                BeforeLoad = scene => {
                    MilliGolf.MilliGolf.doCustomLoad = true;
                },
                LogSceneName = true
            };
            await EverySceneUtil.ForSpecificScenes(courses, p);
        }
    }
}
