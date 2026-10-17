# Place in: xCeru/XIVMechSim -> generate_pluginmaster.py
# Generates pluginmaster.json (the Dalamud custom-repo manifest) from the
# per-plugin manifests under plugins/<InternalName>/<InternalName>.json.
#
# Usage:
#   python generate_pluginmaster.py
#   # then commit pluginmaster.json and push
#
# Unlike anomek/MyDalamudPlugins' version, this does NOT call the GitHub API
# (no PAT needed). Download links point at raw.githubusercontent.com, serving
# the latest.zip committed alongside each plugin's manifest. To cut a new
# release: build, drop a new latest.zip next to the manifest, bump the manifest
# version, run this script, commit, push.

import json
import os
import time

OWNER = "xCeru"
REPO = "XIVMechSim"
BRANCH = "master"

RAW_ZIP = f"https://raw.githubusercontent.com/{OWNER}/{REPO}/{{branch}}/plugins/{{name}}/latest.zip"
RAW_ICON = f"https://raw.githubusercontent.com/{OWNER}/{REPO}/{{branch}}/images/icon.png"

DEFAULTS = {
    "IsHide": False,
    "IsTestingExclusive": False,
    "ApplicableVersion": "any",
}

TRIMMED_KEYS = [
    "Author",
    "Name",
    "Punchline",
    "Description",
    "InternalName",
    "AssemblyVersion",
    "RepoUrl",
    "Tags",
    "DalamudApiLevel",
    "IconUrl",
    "Changelog",
]

DUPLICATES = {
    "DownloadLinkInstall": ["DownloadLinkTesting", "DownloadLinkUpdate"],
}


def load_manifests():
    manifests = []
    plugins_dir = "plugins"
    if not os.path.isdir(plugins_dir):
        return manifests
    for name in sorted(os.listdir(plugins_dir)):
        path = os.path.join(plugins_dir, name, f"{name}.json")
        if not os.path.isfile(path):
            continue
        with open(path, encoding="utf-8") as f:
            manifests.append(json.load(f))
    return manifests


def trim_manifest(plugin):
    return {k: plugin[k] for k in TRIMMED_KEYS if k in plugin}


def add_extra_fields(manifests):
    for manifest in manifests:
        internal = manifest.get("InternalName", "plugin")
        manifest["DownloadLinkInstall"] = RAW_ZIP.format(branch=BRANCH, name=internal)
        for k, v in DEFAULTS.items():
            manifest.setdefault(k, v)
        for source, keys in DUPLICATES.items():
            if source not in manifest:
                continue
            for k in keys:
                manifest.setdefault(k, manifest[source])
        manifest["DownloadCount"] = 0
        manifest["LastUpdate"] = str(int(time.time()))


def write_master(master):
    with open("pluginmaster.json", "w", encoding="utf-8") as f:
        json.dump(master, f, indent=4)
        f.write("\n")
    print(f"Wrote pluginmaster.json with {len(master)} plugin(s).")


def main():
    manifests = load_manifests()
    master = [trim_manifest(m) for m in manifests]
    add_extra_fields(master)
    write_master(master)


if __name__ == "__main__":
    main()
