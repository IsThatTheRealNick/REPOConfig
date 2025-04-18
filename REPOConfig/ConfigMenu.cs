﻿using System;
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

    private static bool hasPopupMenuOpened;
    
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
        
        var repoPopupPage = MenuAPI.CreateREPOPopupPage("Mods", REPOPopupPage.PresetSide.Left, false, true);
        repoPopupPage.scrollView.scrollSpeed = 3f;
        repoPopupPage.maskPadding = repoPopupPage.maskPadding with { top = 35 }; 
        repoPopupPage.onEscapePressed += () => {
            if (hasPopupMenuOpened)
                return true;
            
            if (changedEntries.Count == 0)
                return false;
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?", () => {
                repoPopupPage.ClosePage(true);
                changedEntries.Clear();
                hasPopupMenuOpened = false;
            }, () => hasPopupMenuOpened = false);
            
            hasPopupMenuOpened = true;
                
            return true;
        };
        
        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Back", () => {
            if (changedEntries.Count == 0 || hasPopupMenuOpened)
            {
                repoPopupPage.ClosePage(true);
                return;
            }
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                () =>
                {
                    repoPopupPage.ClosePage(true);
                    changedEntries.Clear();
                    hasPopupMenuOpened = false;
                }, () => hasPopupMenuOpened = false);
            
            hasPopupMenuOpened = true;
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
                
                if (modName.Length > 24)
                {
                    modButton.overrideButtonSize = modButton.GetLabelSize() with { x = 250f };
                    
                    var repoTextScroller = modButton.labelTMP.gameObject.AddComponent<REPOTextScroller>();
                    repoTextScroller.maxCharacters = 24;
                    MenuManager.instance.StartCoroutine(repoTextScroller.Animate());
                }
                
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
                            hasPopupMenuOpened = false;
                        }, () => hasPopupMenuOpened = false);
                    
                    hasPopupMenuOpened = true;
                    return;

                    void OpenPage()
                    {
                        MenuAPI.CloseAllPagesAddedOnTop();
                        
                        var modPage = MenuAPI.CreateREPOPopupPage(modName, REPOPopupPage.PresetSide.Right, false, false, spacing: 5f);
                        modPage.scrollView.scrollSpeed = 3f;
                        modPage.onEscapePressed = () => changedEntries.Count > 0 || hasPopupMenuOpened;
                        
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
                                var absoluteDefaultValue =  Math.Abs((float) entry.BoxedValue);

                                if (absoluteDefaultValue == 0)
                                    min = -(max = 100);
                                else if (absoluteDefaultValue <= .001)
                                    min = -(max = 10f);
                                else if (absoluteDefaultValue <= .01)
                                    min = -(max = 50f);
                                else if (absoluteDefaultValue <= 100)
                                    min = -(max = absoluteDefaultValue * 3f);
                                else
                                     min = -(max = absoluteDefaultValue * 2);
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
                                var absoluteDefaultValue =  Math.Abs((int) entry.BoxedValue);

                                min = absoluteDefaultValue switch
                                {
                                    0 => -(max = 100),
                                    <= 100 => -(max = absoluteDefaultValue * 3),
                                    _ => -(max = absoluteDefaultValue * 2)
                                };
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
                    case ConfigEntry<string> when entry.Description.AcceptableValues is AcceptableValueList<string> acceptableValueList: {
                        modPage.AddElementToScrollView(scrollView => {
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
                    case ConfigEntry<string>:
                    {
                        modPage.AddElementToScrollView(scrollView =>
                        {
                            var repoInputField = MenuAPI.CreateREPOInputField(modName, s => changedEntries[entry] = s, scrollView, delayOnValueChanged: true, defaultValue: (string) entry.BoxedValue);

                            var defaultValue = (string) entry.DefaultValue;
                            repoInputField.inputStringSystem.placeholder = !string.IsNullOrEmpty(defaultValue) ? defaultValue : "<NONE>";

                            return repoInputField.rectTransform;
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
        
        foreach (var plugin in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.Name))
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

        for (var i = 0; i < 1000; i++)
        {
            repoConfigs.Add($"Test {i}", []);
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