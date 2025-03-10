using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MenuLib;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace REPOConfig
{
    [BepInPlugin("nickklmao.repoconfig", MOD_NAME, "1.1.4")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "REPO Config";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);
        
        private static ConfigEntry<bool> showInGame;
        
        private static readonly Dictionary<ConfigEntryBase, object> changedEntries = new();
        
        private static REPOButton currentModButton;
        
        private static void MenuPageMain_StartHook(Action<MenuPageMain> orig, MenuPageMain self)
        {
            var childrenInOrder = self.transform.Cast<Transform>()
                .Where(transform => transform.name.Contains("Menu Button"))
                .OrderByDescending(transform => transform.localPosition.y);
            
            var yPosition = 224f;
            foreach (var child in childrenInOrder)
            {
                if (child.name.Contains("Quit"))
                    yPosition -= 34;
                
                child.localPosition = child.localPosition with { y = yPosition};
                yPosition -= 34;
            }
            
            orig.Invoke(self);
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
        
        private static REPOPopupPage CreateMainModPage()
        {
            changedEntries.Clear();
            currentModButton = null;
            
            var mainModPage = new REPOPopupPage("Mods", mainModPage => {
                mainModPage.SetBackgroundDimming(true);
                mainModPage.SetMaskPadding(new Padding(0, 70, 20, 50));
                mainModPage.AddElementToPage(new REPOButton("Back", () => {
                    var closePage = () =>
                    {
                        mainModPage.ClosePage(true);
                        changedEntries.Clear();
                    };
                    
                    if (changedEntries.Count > 0 && currentModButton != null)
                        currentModButton?.OpenDialog("Unsaved Changes", "You have unsaved changes, are you sure you want to exit?", closePage);
                    else
                        closePage.Invoke();

                }), new Vector2(77f, 34f));
            });
            
            var configEntries = GetModConfigEntries();
            
            for (var i = 0; i < configEntries.Count; i++)
            {
                var (modName, configEntryBases) = configEntries.ElementAt(i);
                
                CreateModPage(modName, configEntryBases, out var modButton);
                
                mainModPage.AddElementToScrollView(modButton, new Vector2(0f, -80f + i * -34f));
            }

            return mainModPage;
        }

        private static void CreateModPage(string modName, ConfigEntryBase[] ConfigEntryBases, out REPOButton modButton)
        {
            var modPage = new REPOPopupPage(modName, modPage => {
                modPage.SetLocalPosition(new Vector2(500.52f, 190.6f));
                modPage.SetSize(new Vector2(310f, 342f));
                modPage.SetMaskPadding(new Padding(0, 70, 0, 50));
            });

            var shortModName = modName;
            if (shortModName.Length > 19)
                shortModName = $"{shortModName[..16]}...";

            var modButtonTemp = modButton = new REPOButton(shortModName, null);
            
            modButton.SetOnClick(() => {
                if (currentModButton == modButtonTemp)
                    return;
                
                var openPage = () => {
                    MenuManager.instance.PageCloseAllAddedOnTop();

                    modPage.ClearButtons();
                    
                    changedEntries.Clear();
                    currentModButton = modButtonTemp;

                    var saveChangesButton = new REPOButton("Save Changes", null);
                    saveChangesButton.SetOnClick(() =>
                    {
                        foreach (var entry in changedEntries)
                            entry.Key.BoxedValue = entry.Value;
                        
                        changedEntries.Clear();
                    });

                    var resetToDefaultButton = new REPOButton("Reset", null);
                    resetToDefaultButton.SetOnClick(() =>
                    {
                        resetToDefaultButton.OpenDialog(
                            $"Reset {modName}?",
                            $"Reset all of {modName}{(modName.ToLower().EndsWith('s') ? "'" : "'s")} settings?",
                            () =>
                            {
                                foreach (var entry in ConfigEntryBases)
                                    entry.BoxedValue = entry.DefaultValue;
                                
                                changedEntries.Clear();
                                currentModButton = null;
                                
                                modButtonTemp.onClick.Invoke();
                            });
                    });

                    modPage.AddElementToPage(saveChangesButton, new Vector2(365, 34f));
                    modPage.AddElementToPage(resetToDefaultButton, new Vector2(560f, 34f));

                    var yPosition = -80f;

                    foreach (var configEntryBase in ConfigEntryBases)
                    {
                        var name = FixNaming(configEntryBase.Definition.Key);
                        var description = configEntryBase.Description.Description;
                        
                        if (description.Length > 60)
                        {
                            var cutOffPoint = description.LastIndexOf(' ', 57);

                            if (cutOffPoint == -1)
                                cutOffPoint = 57;
                            
                            description = $"{description[..cutOffPoint]}...";
                        }
                        
                        switch (configEntryBase)
                        {
                            case ConfigEntry<bool> boolEntry:
                            {
                                modPage.AddElementToScrollView(new REPOToggle(name, b =>
                                {
                                    changedEntries[boolEntry] = b;
                                }, "ON", "OFF", boolEntry.Value), new Vector2(120f, yPosition));
                                yPosition -= 30f;
                                break;
                            }
                            case ConfigEntry<int> intEntry: {
                                var repoSlider = new REPOSlider(name, description, f => {
                                    changedEntries[intEntry] = Convert.ToInt32(f);
                                    
                                }, 0, 1, 0, intEntry.Value);

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
                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 32f : 54f;
                                break;
                            }
                            case ConfigEntry<float> floatEntry:
                            {
                                var repoSlider = new REPOSlider(name, description, f =>
                                {
                                    changedEntries[floatEntry] = f;
                                }, 0, 0, 0, floatEntry.Value);

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

                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 32f : 54f;
                                break;
                            }
                            case ConfigEntry<string> stringEntry:
                            {
                                if (stringEntry.Description.AcceptableValues is not AcceptableValueList<string> valueList || valueList.AcceptableValues.Length == 0)
                                    continue;
                                
                                var repoSlider = new REPOSlider(name, description, i => {
                                    changedEntries[stringEntry] = valueList.AcceptableValues[i];
                                    
                                }, stringEntry.Value, valueList.AcceptableValues);
                                
                                modPage.AddElementToScrollView(repoSlider, new Vector2(15f, yPosition));
                                yPosition -= string.IsNullOrEmpty(repoSlider.description) ? 32f : 54f;
                                break;
                            }
                        }
                    }

                    modPage.OpenPage(true);
                };
                
                if (changedEntries.Count > 0)
                    currentModButton.OpenDialog("Unsaved Changes", "You have unsaved changes, are you sure you want to exit?", openPage);
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
        
        private void Awake()
        {
            showInGame = Config.Bind("General", "Show In Game", true, new ConfigDescription(string.Empty, null, "HideREPOConfig"));
            
            if (!showInGame.Value)
                return;
            
            logger.LogDebug("Hooking `MenuPageMain.Start`");
            new Hook(AccessTools.Method(typeof(MenuPageMain), "Start"), MenuPageMain_StartHook);
            
            MenuAPI.AddElementToMainMenu(new REPOButton("Mods", () => CreateMainModPage().OpenPage(false)), new Vector2(48.3f, 55.5f));
            MenuAPI.AddElementToEscapeMenu(new REPOButton("Mods", () => CreateMainModPage().OpenPage(false)), new Vector2(126f, 86f));
        }
    }
}