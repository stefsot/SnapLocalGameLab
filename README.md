# SnapLocalGameLab

SnapLocalGameLab is an unofficial Windows tool for starting and experimenting with local AI matches in MARVEL SNAP.

## Usage

Requires MARVEL SNAP for Windows x64, .NET Framework 4.8, and the Microsoft Visual C++ v14 runtime for x64.

1. Start MARVEL SNAP and go to the Play/home screen.
2. Run `SnapLocalGameLab.Launcher.exe`, keeping its accompanying DLLs in the same folder.
3. Optionally enter deck codes, locations, and a seed.
4. Click **Start local game** and wait for the board to load.
5. Use **Draw card** or **+2 energy** as needed during the local match.

Blank decks use the game's defaults. Blank locations and seed use random values. Custom locations require exactly three comma-separated internal location IDs.

## Building from source

Open `SnapLocalGameLab.sln` in Visual Studio 2022 with the .NET desktop and C++ desktop workloads, C++/CLI support, the v143 toolset, the .NET Framework 4.8 targeting pack, and a Windows 10 SDK.

Build `Release | x64`, then run `SnapLocalGameLab.Launcher\bin\Release\SnapLocalGameLab.Launcher.exe` with the generated DLLs beside it.

## License

Licensed under the [Apache License, Version 2.0](LICENSE).

Bundled third-party code retains its original licenses and copyright notices.
