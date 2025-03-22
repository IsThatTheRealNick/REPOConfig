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

The currently supported `ConfigEntry` types are `bool`, `int`, `float`, and `string`
- `string` types currently **require** `AcceptableValueList<string>`. *(See "Setting Up Options" below)*

**Setting Up Ranges:**
1. Create a `ConfigEntry<float>` or `ConfigEntry<int>`
2. Bind it using `AcceptableValueRange<float>` or `AcceptableValueRange<int>`:
```
floatEntry = Config.Bind("General", "Float Entry", 2f, new ConfigDescription("", new AcceptableValueRange<float>(2.5f, 10.5f)));
intEntry = Config.Bind("General", "Int Entry", 2, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));
```

**Setting Up Options:**
1. Create a `ConfigEntry<string>`
2. Bind it using `AcceptableValueList<string>`:
```
gamemodeEntry = Config.Bind("General", "Gamemode", "Survival", new ConfigDescription("", new AcceptableValueList<string>("Creative", "Survival", "Adventure Mode")));
```

**Hiding A Setting** *(Case-Sensitive)*
1. Create a `ConfigEntry`
2. Bind it with a tag `HideFromREPOConfig`:
```
floatEntry = Config.Bind("General", "Float Entry", 2f, new ConfigDescription("", null, "HideFromREPOConfig"));
```
</details>

## Note
- Some mods may not have immediate support
