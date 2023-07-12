# ModTool mod support for Unity

<a href="http://u3d.as/Diq">Asset Store</a>  | <a href="https://www.youtube.com/watch?v=9w_WlBPtclg">Youtube</a> | <a href="https://forum.unity3d.com/threads/modtool-mod-support-for-unity.442185/">Forum Thread</a>

ModTool makes it easy to add mod support to your project. It enables modders to use the Unity Editor to create scenes, prefabs and code and export them as mods for your game.

See the included examples and the [Documentation](http://hellomeow.net/modtool/documentation) for more info on how to use ModTool.

## Features

- Let modders use the Unity editor to create scenes, prefabs and code for your game
- Scripts and assemblies are fully supported
- Code validation
- Supports Windows, OS X, Linux, Android, and iOS
- Modular per platform settings
- Mod conflict detection
- Automatic Mod discovery
- Asynchronous discovery and loading of mods

## Limitations

- ModTool relies on AssetBundles, which means there could be some issues if mods are created with the wrong Unity version. The exporter will check if the same version is used and inform the user if that's not the case.
- Unity can't deserialize fields of \[Serializable\] types that have been loaded at runtime. This means that a Mod can't use fields of its own serializable Types in the inspector. Serializable types that aren't loaded at runtime and are part of the game do work.
- Mods have to rely on the game's project settings. This means mods can not define their own new tags, layers and input axes. The created Mod exporter includes the game's project settings
- Supports Unity 2019.4 and up
- Noted an issue, apparently with calling Instantiate within a script in the mod. May need to be managed by calling outside of the mod.

## Acknowledgments

- [Mono.Cecil](https://github.com/jbevain/cecil) by Jb Evain

## How to build

- Open ModTool.sln in Visual Studio
- Add Unity .dll References to the individual PROJECTS not the solution
  - Project > Add Reference > Browse > Click "Browse..." (bottom right) 
  - Find your installed Unity Editor .exe location (ex: C:\Program Files\Unity\Hub\Editor\2019.2.12f1\Editor\)
  - Add the following:
    - UnityEngine.AssetBundleModule.dll
    - UnityEngine.CoreModule.dll
    - UnityEngine.dll
    - UnityEditor.dll
- At the top Build > Build Solution

## Using in the Unity Editor under Linux

The deployment process has been reverted to send the source script files, rather than the compiled dlls, when creating and generating mods. This simplifies
the process when making other changes to the facilities offered. 

Thus this project can be used almost directly by copying it into an existing Unity project. Hard links are useful (avoid soft links, Unity doesn't seem to detect
file changes properly with those) so you can update code in either this repository or in the Unity project and have it immediately available in the other. 
Assuming you create a ModTool directory directly in the Assets of your project, copy the ModTool.Editor and ModTool.Editor.Exporting folders into an Editor folder 
within that. You will also need the ModTool, ModTool.Shared and ModTool.Interface folders added to the ModTool directory. Add in the Mono.Cecil and Resources folders
within the ModTool directory as well (these can be obtained from the version on the Asset Store). The Resources folder contains various scriptable objects, 
including CodeSettings and ModExporter.

Should it be necessary to use dlls in the future, these seem to be available in the Library/ScriptAssemblies folder of the project. You will need
to create additional Assembly Definition files in the ModTool, ModTool.Interface and ModTool.Editor.Exporting to get those required. That in ModTool.Editor.Exporting
needs the ModTool and ModTool.Shared assemblies as dependencies.
