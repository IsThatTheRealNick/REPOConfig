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
    private static readonly Dictionary<ConfigEntryBase, object> changedEntryValues = new();
    
    private static readonly Dictionary<ConfigEntryBase, object> originalEntryValues = new();

    internal static REPOButton lastClickedModButton;

    private static readonly List<REPOButton> currentModButtons = []; 
    
    private static bool hasPopupMenuOpened;
    
    internal static void Initialize()
    {
        MenuAPI.AddElementToMainMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(48.3f, 55.5f)));
        MenuAPI.AddElementToLobbyMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(186f, 32)));
        MenuAPI.AddElementToEscapeMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(126f, 86f)));
    }
    
    private static void CreateModMenu()
    {
        changedEntryValues.Clear();
        
        lastClickedModButton = null;
        
        var repoPopupPage = MenuAPI.CreateREPOPopupPage("Mods", REPOPopupPage.PresetSide.Left, false, true);
        repoPopupPage.scrollView.scrollSpeed = 3f;
        repoPopupPage.maskPadding = repoPopupPage.maskPadding with { top = 35 }; 
        repoPopupPage.onEscapePressed += () => {
            if (hasPopupMenuOpened)
                return false;

            if (changedEntryValues.Count == 0)
                return true;
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?", () => {
                repoPopupPage.ClosePage(true);
                changedEntryValues.Clear();
                hasPopupMenuOpened = false;
            }, () => hasPopupMenuOpened = false);

            hasPopupMenuOpened = true;
            return false;
        };

        repoPopupPage.AddElement(parent =>
        {
            var searchField = MenuAPI.CreateREPOInputField("Mod Search", s =>
            {
                var searchTerm = string.IsNullOrEmpty(s) ? null : s.ToLower().Trim();
                
                foreach (var modButton in currentModButtons)
                    modButton.repoScrollViewElement.visibility = searchTerm == null || modButton.labelTMP.text.ToLower().Contains(searchTerm);
                
                repoPopupPage.scrollView.SetScrollPosition(0);
                
            }, parent, new Vector2(83f, 272f));
            
            searchField.transform.localScale = Vector3.one * .95f;
        });

        CreateModList(repoPopupPage);
        
        repoPopupPage.AddElement(parent => MenuAPI.CreateREPOButton("Back", () => {
            if (changedEntryValues.Count == 0 || hasPopupMenuOpened)
            {
                repoPopupPage.ClosePage(true);
                return;
            }
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                () =>
                {
                    repoPopupPage.ClosePage(true);
                    changedEntryValues.Clear();
                    hasPopupMenuOpened = false;
                }, () => hasPopupMenuOpened = false);
            
            hasPopupMenuOpened = true;
        }, parent, new Vector2(66f, 18f)));

        repoPopupPage.OpenPage(false);
    }

    private static void CreateModList(REPOPopupPage mainModPage)
    {
        currentModButtons.Clear();
        foreach (var (modName, configEntryBases) in GetModConfigEntries())
            mainModPage.AddElementToScrollView(parent => {
                var modButton = MenuAPI.CreateREPOButton(modName, null, parent);
                modButton.labelTMP.fontStyle = FontStyles.Normal;
                
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
                    
                    if (changedEntryValues.Count == 0)
                    {
                        OpenPage();
                        return;
                    }
                    
                    MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                        () =>
                        {
                            changedEntryValues.Clear();
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
                        modPage.onEscapePressed = () =>  !hasPopupMenuOpened && changedEntryValues.Count == 0;
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Save Changes", () =>
                            {
                                var cachedEntries = changedEntryValues.ToArray();
                                changedEntryValues.Clear();

                                foreach (var (key, value) in cachedEntries)
                                {
                                    key.BoxedValue = value;
                                    originalEntryValues[key] = value;
                                }
                                
                            }, mainPageParent, new Vector2(370f, 18f));
                        });
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Revert", () =>
                            {
                                if (changedEntryValues.Count == 0)
                                    return;
                                
                                changedEntryValues.Clear();
                                OpenPage();
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

                                    changedEntryValues.Clear();
                                    OpenPage();
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
                
                currentModButtons.Add(modButton);
                return modButton.rectTransform;
            });
    }

    private static void CreateModEntries(REPOPopupPage modPage, ConfigEntryBase[] configEntryBases)
    {
        var sectionGroups = configEntryBases.GroupBy(entry => entry.Definition.Section);

        foreach (var group in sectionGroups)
        {
            modPage.AddElementToScrollView(scrollView =>
            {
                var repoLabel = MenuAPI.CreateREPOLabel(FixNaming(group.Key), scrollView);
                repoLabel.labelTMP.fontStyle = FontStyles.Bold;
                return repoLabel.rectTransform;
            });

            foreach (var entry in group)
            {
                var modName = FixNaming(entry.Definition.Key);
                //var description = Entry.showDescriptions.Value ? entry.Description.Description.Replace("\n", string.Empty) : string.Empty;

                originalEntryValues.Remove(entry);
                originalEntryValues.Add(entry, entry.BoxedValue);
                
                switch (entry)
                {
                    case ConfigEntry<bool>:
                    {
                        modPage.AddElementToScrollView(scrollView =>
                        {
                            var repoToggle = MenuAPI.CreateREPOToggle(modName, b =>
                            {
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && b == (bool) originalValue)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = b;
                            }, scrollView, defaultValue: (bool)entry.BoxedValue);
                            repoToggle.labelTMP.fontStyle = FontStyles.Normal;
                            return repoToggle.rectTransform;
                        });
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
                                
                                precision = Mathf.Max(GetDecimalPlaces(min), GetDecimalPlaces(max), GetDecimalPlaces((float) entry.DefaultValue), 2);
                            }
                            else
                            {
                                var absoluteDefaultValue = Math.Abs((float) entry.BoxedValue);

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

                            var repoSlider = MenuAPI.CreateREPOSlider(modName, string.Empty, f => //description
                            {
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && Math.Abs(f - (float) originalValue) < float.Epsilon)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = f;
                            }, scrollView, defaultValue: (float)entry.BoxedValue, min: min, max: max, precision: precision);
                            repoSlider.descriptionTMP.fontStyle = repoSlider.labelTMP.fontStyle = FontStyles.Normal;
                            
                            /*if (description.Length <= 43)
                                return repoSlider.rectTransform;*/
                            
                            //repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            //repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            /*repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());*/


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
                            
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, string.Empty, i => //description
                            {
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && i == (int) originalValue)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = i;
                            }, scrollView, defaultValue: (int) entry.BoxedValue, min: min, max: max);
                            repoSlider.descriptionTMP.fontStyle = repoSlider.labelTMP.fontStyle = FontStyles.Normal;
                            
                            /*if (description.Length <= 43)
                                return repoSlider.rectTransform;*/
                            
                            /*repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());*/
                            
                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                    case ConfigEntry<string> when entry.Description.AcceptableValues is AcceptableValueList<string> acceptableValueList: {
                        modPage.AddElementToScrollView(scrollView => {
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, string.Empty, s => //description
                            {
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && s == (string) originalValue)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = s;
                            }, scrollView, acceptableValueList.AcceptableValues, (string)entry.BoxedValue);
                            repoSlider.descriptionTMP.fontStyle = repoSlider.labelTMP.fontStyle = FontStyles.Normal;
                            
                            /*if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());*/
                            
                            return repoSlider.rectTransform;
                        });
                        break;
                    }
                    case ConfigEntry<string>:
                    {
                        modPage.AddElementToScrollView(scrollView =>
                        {
                            var defaultValue = (string) entry.DefaultValue;
                            
                            var repoInputField = MenuAPI.CreateREPOInputField(modName, s =>
                            {
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && s == (string) originalValue)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = s;
                            }, scrollView, Vector2.zero, false, !string.IsNullOrEmpty(defaultValue) ? defaultValue : "<NONE>", (string) entry.BoxedValue);
                            repoInputField.labelTMP.fontStyle = repoInputField.inputStringSystem.inputTMP.fontStyle = FontStyles.Normal;
                            
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
                            var repoSlider = MenuAPI.CreateREPOSlider(modName, string.Empty, i => //description
                            {
                                var enumValue = Enum.Parse(enumType, values[i]);
                                
                                if (originalEntryValues.TryGetValue(entry, out var originalValue) && enumValue == originalValue)
                                {
                                    changedEntryValues.Remove(entry);
                                    return;
                                }
                                
                                changedEntryValues[entry] = enumValue;
                            }, scrollView, values, entry.BoxedValue.ToString());
                            repoSlider.descriptionTMP.fontStyle = repoSlider.labelTMP.fontStyle = FontStyles.Normal;
                            
                            /*if (description.Length <= 43)
                                return repoSlider.rectTransform;
                            
                            repoSlider.descriptionTMP.maxVisibleCharacters = repoSlider.repoTextScroller.maxCharacters = 43;
                            repoSlider.repoTextScroller.scrollingSpeedInSecondsPerCharacter = Entry.descriptionScrollSpeed.Value;
                                
                            repoSlider.repoTextScroller.endWaitTime = repoSlider.repoTextScroller.initialWaitTime = 5f;
                            repoSlider.repoTextScroller.startWaitTime = 3f;

                            repoSlider.descriptionTMP.alignment = TextAlignmentOptions.Left;
                            modPage.StartCoroutine(repoSlider.repoTextScroller.Animate());*/
                            
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