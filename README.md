# Signalboy Plugin for Unity
## Dependencies
* [Signalboy Android library](https://github.com/kshrana/signalboy-android) (`de.kishorrana.signalboy_android`)

## Prerequisites
### EDM4U
The companion native Android library is distributed as a Maven package. To install it (and its dependencies) you will need to install the [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver) (formerly Play Services Resolver / Jar Resolver) into your Unity-project.

Perform the following steps to setup EDM4U:
* Make sure that EDM4U is installed in your Unity-project. ([Download latest release here (.unitypackage)](https://github.com/googlesamples/unity-jar-resolver/raw/master/external-dependency-manager-latest.unitypackage))
* Currently you'll additionally have to provide EDM4U with the latest release of the companion Android library using a **local Maven repo** in your project's Assets:
  * Download the latest Maven repo[^signalboy-android-releases] and extract it to into your Unity Project root at: `<your-unity-project>/Assets/Signalboy/Editor/GeneratedLocalRepo/` (creating the filepath, if necessary)
* (may be optional if EDM4U's Auto-Resolution was successful) In the Unity Editor with your project open, select _Assets -> External Dependency Manager -> Android Resolver -> Force Resolve_

## Installation
### Install via GIT URL
Install latest release of this plugin using _Unity's Package Manager_:
* Either: Select _Window -> Package Manager_ and click the _"+"-Button -> Add package from git URL..._ and enter `https://github.com/kshrana/signalboy-unity.git`
* …or: add this plugin as a package by modifying your `manifest.json` file found at `<your-unity-project>/Packages/manifest.json` to include it as a dependency. Merge the snippet below with your `manifest.json` file to reference it.
```
{
	"dependencies": {
		...
		"de.kishorrana.signalboy_unity" : "https://github.com/kshrana/signalboy-unity.git",
		...
	}
}
```

## Upgrading the companion Android library
To update the native library used by this plugin, perform the following steps:
* Bump `de.kishorrana.signalboy_android`'s version requirement in [SignalboyDependencies.xml](./Editor/SignalboyDependencies.xml).
* Users of this Unity Package will have to manually re-download the latest Maven repo[^signalboy-android-releases] and extract it to an adequate location (s. [Installation](#installation)).

[^signalboy-android-releases]: Find the latest Maven repo here: [GitHub | kshrana/signalboy-android | Releases](https://github.com/kshrana/signalboy-android/releases/latest))

## TODO
- [ ]: Update installation git-urls with urls referencing the latest release.
