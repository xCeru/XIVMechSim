# Installation

Add the following custom repository in Dalamud Settings → Experimental tab → Custom Plugin Repositories:

```
https://raw.githubusercontent.com/xCeru/XIVMechSim/master/pluginmaster.json
```

Click `+`, then `Save`. Open `/xlplugins`, switch to **All Plugins**, and install **AnoMech**.

## Supported plugins

- **AnoMech** — Another FFXIV mechanics simulator. Solo practice for ultimates from the safety of your inn.
  - P3 Ultimate Relativity (NAUR / LPDU strat choice)
  - P3 Apocalypse (NAUR / LPDU strat choice)

## Cutting a new release (run locally)

You do not need the GitHub Actions `PAT` secret. Everything builds and releases from this repo directly:

1. **Build the plugin**
   ```bash
   dotnet build AnoMech/AnoMech.csproj -c Release
   ```
   Output lands in `AnoMech/bin/Release/AnoMech/`.

2. **Package the zip**
   Zip the contents of `AnoMech/bin/Release/AnoMech/` (the DLL, deps, manifest, and dependency DLLs) into
   `plugins/AnoMech/latest.zip`.

3. **Update the plugin manifest**
   Edit `plugins/AnoMech/AnoMech.json` — bump `AssemblyVersion` and add a changelog line.

4. **Regenerate the Dalamud repo manifest**
   ```bash
   python generate_pluginmaster.py
   ```
   This rewrites `pluginmaster.json` with the new version and download links.

5. **Commit and push**
   ```bash
   git add plugins/AnoMech/AnoMech.json plugins/AnoMech/latest.zip pluginmaster.json
   git commit -m "Release AnoMech vX.Y.Z.W"
   git push origin master
   ```

The Dalamud installer picks up the new version from `pluginmaster.json` automatically on its next refresh.

## Dalamud repo URL

```
https://raw.githubusercontent.com/xCeru/XIVMechSim/master/pluginmaster.json
```
