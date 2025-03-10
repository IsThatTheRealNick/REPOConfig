# REPO Config
Edit mod configs in-game!

## Installation

1. **Ensure you have BepInEx installed**

   - Download BepInEx from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases)
   - Extract it into your R.E.P.O. game directory

2. **Download RepoConfig**

   - Download the latest release from [Thunderstore](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)
   - Place the `RepoConfig.dll` inside `BepInEx/plugins/`

### For Developers
<details><summary>Click To Expand</summary><br>

If you've referenced an older version of this mod, the attribute is obsolete and will be removed in the future.

**Setting Up Ranges:**
1. Create a `ConfigEntry<float>` or `ConfigEntry<int>`
2. Bind it using `AcceptableValueRange<float>` or `AcceptableValueRange<int>`:
```
floatEntry = Config.Bind("General", "Float Entry", 2f, new ConfigDescription(null, new AcceptableValueRange<float>(2.5f, 10.5f)));
intEntry = Config.Bind("General", "Int Entry", 2, new ConfigDescription(null, new AcceptableValueRange<int>(0, 100)));
```

**Setting Up Options:**
1. Create a `ConfigEntry<string>`
2. Bind it using `AcceptableValueList<string>`:
```
gamemodeEntry = Config.Bind("General", "Gamemode", "Survival", new ConfigDescription(null, new AcceptableValueList<string>("Creative", "Survival", "Adventure Mode")));
```
</details>

## Note
- Some mods may not have immediate support