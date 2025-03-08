# REPO Config
Edit mod configs in-game!

### For Developers
<details><summary>Click To Expand</summary><br>

In order for your `ConfigEntry` to be listed under your mod, it must meet this criteria:  
1. The ConfigEntry is of type `bool`, `int`, or `float`
2. The field/property is static OR it's instanced under your entry class (EntryClass : BaseUnityPlugin)

If you've referenced an older version of this mod, the attribute is currently obselete and won't do anything.  
The proper way to set up ranges can be done by using `AcceptableValueRange` when defining your config entry:

```
floatEntry = Config.Bind("General", "Float Entry", 2f, new ConfigDescription(null, new AcceptableValueRange<float>(2.5f, 10.5f)));
intEntry = Config.Bind("General", "Int Entry", 2, new ConfigDescription(null, new AcceptableValueRange<int>(0, 100)));
```
</details>

## Note
- Some mods may not have immediate support