using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using MenuLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPOConfig;

internal sealed class ConfigMenu
{
    private static readonly Dictionary<ConfigEntryBase, object> changedEntries = new();
    private static REPOButton currentPageModButton;

    internal static void Initialize()
    {
        MenuAPI.AddElementToMainMenu(new REPOButton("Mods", () => CreateConfigMenu().OpenPage(false)), new Vector2(48.3f, 55.5f));
        MenuAPI.AddElementToEscapeMenu(new REPOButton("Mods", () => CreateConfigMenu().OpenPage(false)), new Vector2(126f, 86f));
    }
    
    private static REPOPopupPage CreateConfigMenu()
    {
        changedEntries.Clear();
        currentPageModButton = null;

        var mainModPage = new REPOPopupPage("Mods").SetBackgroundDimming(true).SetMaskPadding(new Padding(0, 70, 20, 50));
        
        mainModPage.AddElementToPage(new REPOButton("Back", () => {
            var closePage = () => {
                mainModPage.ClosePage(true);
                changedEntries.Clear();
                currentPageModButton = null;
            };

            if (changedEntries.Count > 0)
                MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?", "Yes", closePage, "No");
            else
                closePage.Invoke();

        }), new Vector2(77f, 34f));
            
        var configEntries = GetModConfigEntries();
        var keys = configEntries.Keys.ToArray();
        
        for (var i = 0; i < keys.Length; i++)
        {
            var modName = keys[i];
            var configEntryBases = configEntries[modName];
                
            CreateModPage(modName, configEntryBases, out var modButton);
                
            mainModPage.AddElementToScrollView(modButton, new Vector2(0f, -80f + i * -34f));
        }

        return mainModPage;
    }
    
    private static Dictionary<string, ConfigEntryBase[]> GetModConfigEntries()
        {
            var repoConfigs = new Dictionary<string, ConfigEntryBase[]>();
            
            foreach (var plugin in Chainloader.PluginInfos.Values)
            {
                var configEntries = new List<ConfigEntryBase>();

                foreach (var configEntryBase in plugin.Instance.Config.Select(configEntry => configEntry.Value))
                {
                    
                    if (configEntryBase.Description != null && configEntryBase.Description.Tags.Contains("HideREPOConfig"))
                        continue;
                    
                    configEntries.Add(configEntryBase);    
                }
                
                
                if (configEntries.Count > 0)
                    repoConfigs.TryAdd(FixNaming(plugin.Metadata.Name), configEntries.ToArray());
            }

            return repoConfigs;
        }

    private static void CreateModPage(string modName, ConfigEntryBase[] configEntryBases, out REPOButton modButton)
        {
            var modPage = new REPOPopupPage(modName, modPage => {
                modPage.SetPosition(new Vector2(500.52f, 190.6f));
                modPage.SetSize(new Vector2(310f, 342f));
                modPage.SetMaskPadding(new Padding(0, 70, 0, 50));
            });

            var shortName = modName;
            if (shortName.Length > 19)
                shortName = $"{shortName[..16]}...";

            var modButtonTemp = modButton = new REPOButton(shortName, null);
            
            modButton.SetOnClick(() => {
                if (currentPageModButton == modButtonTemp)
                    return;
                
                var openPage = () => {
                    MenuManager.instance.PageCloseAllAddedOnTop();

                    modPage.ClearButtons();
                    
                    changedEntries.Clear();
                    currentPageModButton = modButtonTemp;

                    var saveChangesButton = new REPOButton("Save Changes", null)
                        .SetOnClick(() => {
                            foreach (var entry in changedEntries)
                                entry.Key.BoxedValue = entry.Value;
                            
                            changedEntries.Clear();
                        });

                    var resetToDefaultButton = new REPOButton("Reset", null);
                    resetToDefaultButton.SetOnClick(() => {
                        MenuAPI.OpenPopup($"Reset {modName}", Color.red, $"Reset all of {modName}{(modName.ToLower().EndsWith('s') ? "'" : "'s")} settings?", "Yes", () => {
                                foreach (var entry in configEntryBases)
                                    entry.BoxedValue = entry.DefaultValue;
                                
                                changedEntries.Clear();
                                currentPageModButton = null;
                                modButtonTemp.onClick.Invoke();
                            }, "No");
                    });

                    modPage.AddElementToPage(saveChangesButton, new Vector2(365, 34f));
                    modPage.AddElementToPage(resetToDefaultButton, new Vector2(560f, 34f));

                    var yPosition = -80f;

                    for (var i = 0; i < configEntryBases.Length; i++)
                    {
                        var configEntryBase = configEntryBases[i];
                        var nextEntryType = configEntryBases.ElementAtOrDefault(i + 1)?.SettingType;
                        
                        var name = FixNaming(configEntryBase.Definition.Key);
                        var description = configEntryBase.Description.Description.Replace('\n', ' ');

                        switch (configEntryBase)
                        {
                            case ConfigEntry<bool> boolEntry: {
                                modPage.AddElementToScrollView(
                                    new REPOToggle(name, b => { changedEntries[boolEntry] = b; }, "ON", "OFF",
                                        boolEntry.Value), new Vector2(120f, yPosition));
                                yPosition -= 30f;
                                break;
                            }
                            case ConfigEntry<int> intEntry:
                            {
                                var repoSlider = new REPOSlider(name, description,
                                    f => { changedEntries[intEntry] = Convert.ToInt32(f); }, 0, 1, 0, intEntry.Value);

                                if (intEntry.Description.AcceptableValues is AcceptableValueRange<int> range)
                                {
                                    repoSlider.SetMin(range.MinValue);
                                    repoSlider.SetMax(range.MaxValue);
                                }
                                else
                                {
                                    var defaultValue = (int)intEntry.DefaultValue;
                                    var estimatedRangeMax = defaultValue switch
                                    {
                                        0 => 10,
                                        _ => defaultValue * 2
                                    };

                                    repoSlider.SetMin(-estimatedRangeMax);
                                    repoSlider.SetMax(estimatedRangeMax);
                                }

                                modPage.AddElementToScrollView(repoSlider, new Vector2(15f, yPosition));
                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 34f : 54f;
                                break;
                            }
                            case ConfigEntry<float> floatEntry: {
                                var repoSlider = new REPOSlider(name, description,
                                    f => { changedEntries[floatEntry] = f; }, 0, 0, 0, floatEntry.Value);

                                var defaultValue = (float)floatEntry.DefaultValue;

                                switch (floatEntry.Description.AcceptableValues)
                                {
                                    case AcceptableValueRange<float> floatRange:
                                        repoSlider.SetMin(floatRange.MinValue);
                                        repoSlider.SetMax(floatRange.MaxValue);
                                        break;
                                    case AcceptableValueRange<int> intRange:
                                        repoSlider.SetMin(intRange.MinValue);
                                        repoSlider.SetMax(intRange.MaxValue);
                                        break;
                                    default:
                                    {
                                        var estimatedRangeMax = defaultValue switch
                                        {
                                            0 => 10,
                                            < 1f => 2f,
                                            _ => defaultValue * 2f
                                        };

                                        repoSlider.SetMin(-estimatedRangeMax);
                                        repoSlider.SetMax(estimatedRangeMax);
                                        break;
                                    }
                                }

                                var defaultValueAsString = defaultValue.ToString(CultureInfo.InvariantCulture);

                                var decimalIndex = defaultValueAsString.IndexOf('.');

                                if (decimalIndex == -1)
                                    repoSlider.SetPrecision(0);
                                else
                                    repoSlider.SetPrecision(defaultValueAsString.Length - decimalIndex - 1);

                                modPage.AddElementToScrollView(repoSlider, new Vector2(15f, yPosition));

                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 34f : 54f;
                                break;
                            }
                            case ConfigEntry<string> stringEntry:
                            {
                                if (stringEntry.Description.AcceptableValues is not AcceptableValueList<string>
                                        valueList || valueList.AcceptableValues.Length == 0)
                                    continue;

                                var repoSlider = new REPOSlider(name, description,
                                    @int => { changedEntries[stringEntry] = valueList.AcceptableValues[@int]; },
                                    stringEntry.Value, valueList.AcceptableValues);

                                modPage.AddElementToScrollView(repoSlider, new Vector2(15f, yPosition));
                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 34f : 54f;
                                break;
                            }
                            case ConfigEntry<Key> keyEntry:
                            {
                                var repoKeybind = new REPOKeybind(name, key => changedEntries[keyEntry] = key,
                                    keyEntry.Value);

                                modPage.AddElementToScrollView(repoKeybind, new Vector2(15f, yPosition));
                                yPosition -= 34f;
                                break;
                            }
                        }
                    }

                    modPage.OpenPage(true);
                };

                if (changedEntries.Count > 0)
                    MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?", "Yes", openPage, "No");
                else
                    openPage.Invoke();
            });
        }
    
    private static string FixNaming(string input)
    {
        input = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        input = Regex.Replace(input, "([A-Z])([A-Z][a-z])", "$1 $2");
        input = Regex.Replace(input, @"\s+", " ");
        input = Regex.Replace(input, @"([A-Z]\.)\s([A-Z]\.)", "$1$2");

        return input.Trim();
    }
}