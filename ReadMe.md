# EverySceneUtil

This is a developer tool mod which can load almost every scene in the game and run scripts on each one.

Several examples of these methods exist in the source code for this mod.

## Methods
- The **ForEveryScene** method will load a predefined sequence of nearly every vanilla scene. This list currently does not include dream bosses (including NKG and Radiance), dream plats, Shrine of Believers rooms, dreamers, or godhome arenas, but these could potentially be added in the future. Cinematics and other non-gameplay scenes are not planned.
- The **ForSpecificScenes** method is capable of loading a custom list of scenes which you provide. This method is intended to be used to load scenes that are added by other mods, though the scenes do need to exist for this to work (the mod is installed, you are in a plando save file, etc).

## Parameters
- **ForEveryScene** is a lengthy process and requires a KeyCode killswitch, which will halt the loads when held down
- **ForSpecificScenes** takes a (string, string) array which includes the scene name and the gate from which the knight will enter.
- Both methods take an *ESU_Params* struct for more complex parameter info:
  - **BeforeLoad** is an Action which takes a string as input. This will be called if it exists before loading the scene and will pass the scene name as the parameter.
  - **OnLoad** is an Action which is called once the scene has finished loading.
  - **LogSceneName** is a bool which, when true, will write the scene name to the mod log along with any conditional data according to the two parameters below.
  - **AdditionalScenes** allows you to specify how you want to handle any scene that would naturally load a secondary additional scene, such as boss scenes.
	- **Ignore** will load each scene only once without modifying its state.
	- **AlwaysLoadWithExtras** will ensure that every additional scene gets loaded without any excess repetition (Ruins1_24 will be loaded only once with mageLordDefeated=false in order to load Ruins1_24_boss)
	- **WithAndWithoutExtras** will load several versions of each applicable scene to include and exclude additional scenes (Ruins1_24 will be loaded twice with mageLordDefeated as both true and false)
  - **Infection** allows you to specify how rooms with infection are handled
	- **Ignore** will leave infection in its current state and load each room only once
	- **NeverInfected** will only load uninfected variations of every room
	- **AlwaysInfected** will only load infected variations of every room
	- **Both** will load applicable rooms twice with and without infection

Most data altered by parameters should be reverted to its original state when scene loading is complete.

For Ruins2_03 (Watcher Knights) and Mines_18 (Crystal Guardian), the AdditionalScenes parameter is treated as Ignore because these rooms use a more complicated system that a simple PlayerData bool to determine how the secondary scene should load.
