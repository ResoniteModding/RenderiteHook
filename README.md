<p align="center">
    <img src="https://raw.githubusercontent.com/ResoniteModding/RenderiteHook/master/icon.png">
</p>

# RenderiteHook
[![Thunderstore Version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fexperimental%2Fpackage%2FResoniteModding%2FRenderiteHook%2F&query=%24.latest.version_number&label=Thunderstore&style=flat&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/resonite/p/ResoniteModding/RenderiteHook/)
[![Thunderstore Downloads](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FResoniteModding%2FRenderiteHook%2F&query=%24.downloads&label=downloads&style=flat&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/resonite/p/ResoniteModding/RenderiteHook/)
[![NuGet Version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fnuget-modding.resonite.net%2Fv3%2Fregistration%2Fresonitemodding.renderitehook%2Findex.json&query=%24.items%5B0%5D.upper&label=NuGet&style=flat&logo=nuget&logoColor=white&color=004880)](https://nuget-modding.resonite.net/packages/resonitemodding.renderitehook)
[![Build](https://img.shields.io/github/actions/workflow/status/ResoniteModding/RenderiteHook/build.yml?style=flat&logo=github)](https://github.com/ResoniteModding/RenderiteHook/actions/workflows/build.yml)
[![License](https://img.shields.io/badge/license-MIT--0-blue?style=flat)](#license)

A [Resonite](https://resonite.com/) mod that allows passing custom command-line arguments to the Resonite's Unity renderer process.

Only needed for renderer-side modifications - most game logic lives in FrooxEngine and doesn't require this plugin.

## Installation (Manual)
1. Install [BepisLoader](https://github.com/ResoniteModding/BepisLoader) for Resonite.
2. Download the latest release ZIP file (e.g., `ResoniteModding-RenderiteHook-1.2.0.zip`) from the [Releases](https://github.com/ResoniteModding/RenderiteHook/releases) page.
3. Extract the ZIP and copy the `plugins` folder to your BepInEx folder in your Resonite installation directory:
   - **Default location:** `C:\Program Files (x86)\Steam\steamapps\common\Resonite\BepInEx\`
4. Start the game. If you want to verify that the mod is working you can check your BepInEx logs.

## License

This project is licensed under the MIT No Attribution license. See [LICENSE](LICENSE) for details.
