using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using MenuLib;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;

namespace REPOConfig;

internal sealed class ConfigMenu
{
    private static readonly Dictionary<ConfigEntryBase, object> changedEntries = new();

    internal static REPOButton lastClickedModButton;
    
    internal static void Initialize()
    {
        MenuAPI.AddElementToMainMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(48.3f, 55.5f)));
        MenuAPI.AddElementToLobbyMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(186f, 32)));
        MenuAPI.AddElementToEscapeMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(126f, 86f)));
    }

    private static void CreateModMenu()
    {
        changedEntries.Clear();
        lastClickedModButton = null;
        
        var repoPopupPage = MenuAPI.CreateREPOPopupPage("Mods", REPOPopupPage.PresetSide.Left, true, false);

        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Back", () => {
            if (changedEntries.Count == 0)
            {
                repoPopupPage.ClosePage(true);
                return;
            }
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                () =>
                {
                    repoPopupPage.ClosePage(true);
                    changedEntries.Clear();
                });
        }, parent, new Vector2(66f, 18f)));

        CreateModList(repoPopupPage);

        repoPopupPage.OpenPage(false);
    }

    private static void CreateModList(REPOPopupPage mainModPage)
    {
        foreach (var (modName, configEntryBases) in GetModConfigEntries())
            mainModPage.AddElementToScrollView(parent =>
            {
                var modButton = MenuAPI.CreateREPOButton(modName, null, parent);
                
                modButton.onClick = () =>
                {
                    if (lastClickedModButton == modButton)
                        return;
                    
                    if (changedEntries.Count == 0)
                    {
                        OpenPage();
                        return;
                    }
            
                    MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                        () =>
                        {
                            changedEntries.Clear();
                            OpenPage();
                        });
                    
                    return;

                    void OpenPage()
                    {
                        MenuAPI.CloseAllPagesAddedOnTop();
                        
                        var modPage = MenuAPI.CreateREPOPopupPage(modName, REPOPopupPage.PresetSide.Right, shouldCachePage: false, spacing: 5f);
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Save Changes", () =>
                            {
                                var cachedEntries = changedEntries.ToArray();
                                changedEntries.Clear();
                                
                                foreach (var (key, value) in cachedEntries)
                                    key.BoxedValue = value;
                                
                            }, mainPageParent, new Vector2(370f, 18f));
                        });
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Revert", () =>
                            {
                                if (changedEntries.Count == 0)
                                    return;
                                
                                changedEntries.Clear();
                                lastClickedModButton = null;
                                modButton.onClick.Invoke();
                            }, mainPageParent, new Vector2(585f, 18f));
                        });
                        
                        modPage.AddElementToScrollView(scrollView => {
                            var resetButton = MenuAPI.CreateREPOButton("Reset To Default", () => {
                                MenuAPI.OpenPopup($"Reset {modName}'{(modName.ToLower().EndsWith('s') ? string.Empty : "s")} settings?", Color.red, "Are you sure you want to reset all settings back to default?", ResetToDefault);
                                
                                return;

                                void ResetToDefault()
                                {
                                    foreach (var configEntryBase in configEntryBases)
                                        configEntryBase.BoxedValue = configEntryBase.DefaultValue;

                                    changedEntries.Clear();
                                    lastClickedModButton = null;
                                    modButton.onClick.Invoke();
                                }
                            }, scrollView);

                            resetButton.rectTransform.localPosition = new Vector2((modPage.maskRectTransform.rect.width - resetButton.GetLabelSize().x) * .5f, 0);
                        
                            return resetButton.rectTransform;
                        });
                    
                        modPage.AddElementToScrollView(scrollView => MenuAPI.CreateREPOSpacer(scrollView, size: new Vector2(0, 10)).rectTransform);
                    
                        CreateModEntries(modPage, configEntryBases);
                        
                        modPage.OpenPage(true);
                        
                        lastClickedModButton = modButton;
                    }
                };
                

                return modButton.rectTransform;
            });
    }

    private static void CreateModEntries(REPOPopupPage modPage, ConfigEntryBase[] configEntryBases)
    {
        var sectionGroups = configEntryBases.GroupBy(entry => entry.Definition.Section);

        foreach (var group in sectionGroups)
        {
            modPage.AddElementToScrollView(scrollView => MenuAPI.CreateREPOLabel(FixNaming(group.Key), scrollView).rectTransform);

            foreach (var entry in group)
            {
                var modName = FixNaming(entry.Definition.Key);
                var description = Entry.showDescriptions.Value ? entry.Description.Description.Replace("\n", string.Empty) : string.Empty;
                
                switch (entry)
                {
                    case ConfigEntry<bool>:
                    {
                        modPage.AddElementToScrollView(scrollView => MenuAPI.CreateREPOToggle(modName, b => changedEntries[entry] = b, scrollView, defaultValue: (bool) entry.BoxedValue).rectTransform);
                        break;
                    }
                    case ConfigEntry<float>: {
                        modPage.AddElementToScrollView(scrollView => {
                            float min, max;
                            var precision = 2;
                            
                            if (entry.Description.AcceptableValues is AcceptableValueRange<float> acceptableValueRange)
                            {
                                min = acceptableValueRange.MinValue;
                                max = acceptableValueRange.MaxValue;
                                
                                precision = Mathf.Max(GetDecimalPlaces(min), GetDecimalPlaces(max), GetDecimalPlaces((float) entry.DefaultValue));
                            }
                            else
                            {
                                min = -100;
                                max = 100;
                            }

                            var repoSlider = MenuAPI.CreateREPOSlider(modName, description, f => changedEntries[entry] = f, scrollView, defaultValue: (float)entry.BoxedValue, min: min, max: max, precision: precision);

                            if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());


                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                    case ConfigEntry<int>: {
                        modPage.AddElementToScrollView(scrollView => {
                            int min;
                            int max;
                            
                            if (entry.Description.AcceptableValues is AcceptableValueRange<int> acceptableValueRange)
                            {
                                min = acceptableValueRange.MinValue;
                                max = acceptableValueRange.MaxValue;
                            }
                            else
                            {
                                min = -100;
                                max = 100;
                            }
                            
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, description, i => changedEntries[entry] = i, scrollView, defaultValue: (int) entry.BoxedValue, min: min, max: max);
                            
                            if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());
                            
                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                    case ConfigEntry<string>: {
                        if (entry.Description.AcceptableValues is not AcceptableValueList<string> acceptableValueList)
                            continue;
                        
                        modPage.AddElementToScrollView(scrollView =>
                        {
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, description, (string s) => changedEntries[entry] = s, scrollView, acceptableValueList.AcceptableValues, (string)entry.BoxedValue);

                            if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());
                            
                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                    case not null when entry.SettingType.IsSubclassOf(typeof(Enum)):
                    {
                        var enumType = entry.SettingType;
                        var values = Enum.GetNames(enumType);
                        
                        modPage.AddElementToScrollView(scrollView =>
                        {
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, description, i => changedEntries[entry] = Enum.Parse(enumType, values[i]), scrollView, values, entry.BoxedValue.ToString());
                            
                            if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());
                            
                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                }
            }
            
            modPage.AddElementToScrollView(scrollView => MenuAPI.CreateREPOSpacer(scrollView, size: new Vector2(0, 20)).rectTransform);   
        }
    }
    
    private static Dictionary<string, ConfigEntryBase[]> GetModConfigEntries()
    {
        var repoConfigs = new Dictionary<string, ConfigEntryBase[]>();
        
        foreach (var plugin in Chainloader.PluginInfos.Values)
        {
            var configEntries = new List<ConfigEntryBase>();

            foreach (var configEntryBase in plugin.Instance.Config.Select(configEntry => configEntry.Value))
            {
                var tags = configEntryBase.Description?.Tags;
                
                if (tags != null && (tags.Contains("HideREPOConfig") || tags.Contains("HideFromREPOConfig")))
                    continue;
                
                configEntries.Add(configEntryBase);
            }
            
            if (configEntries.Count > 0)
                repoConfigs.TryAdd(FixNaming(plugin.Metadata.Name), configEntries.ToArray());
        }

        return repoConfigs;
    }

    private static string FixNaming(string input)
    {
        input = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        input = Regex.Replace(input, "([A-Z])([A-Z][a-z])", "$1 $2");
        input = Regex.Replace(input, @"\s+", " ");
        input = Regex.Replace(input, @"([A-Z]\.)\s([A-Z]\.)", "$1$2");

        return input.Trim();
    }

    private static int GetDecimalPlaces(float value)
    {
        var valueAsString = value.ToString(CultureInfo.InvariantCulture);

        var decimalPoint = valueAsString.IndexOf('.');

        return decimalPoint == -1 ? 0 : valueAsString[(decimalPoint + 1)..].Length;
    }
}