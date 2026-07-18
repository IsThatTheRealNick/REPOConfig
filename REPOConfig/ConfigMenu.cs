using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using MenuLib;
using MenuLib.MonoBehaviors;
using REPOConfig.Strategies;
using TMPro;
using UnityEngine;

namespace REPOConfig;

internal sealed class ConfigMenu
{
    private static readonly ConfigEntryStore configEntryStore = new();
    
    private static readonly IDictionary<Type, IConfigEntryStrategy> configEntryStrategies = CreateStrategiesDictionary();

    internal static REPOButton lastClickedModButton;

    private static readonly List<REPOButton> currentModButtons = []; 
    
    private static bool hasPopupMenuOpened;

    private static Dictionary<Type, IConfigEntryStrategy> CreateStrategiesDictionary()
    {
        List<IConfigEntryStrategy> strategies =
        [
            new StringConfigEntryStrategy(),
            new BooleanConfigEntryStrategy(),
            new IntConfigEntryStrategy(),
            new FloatConfigEntryStrategy()
        ];
        
        return strategies.ToDictionary(s => s.TargetType, s => s);
    }

    internal static void Initialize()
    {
        //Main Menu Button is created in 'Entry.MenuPageMain_StartHook'
        
        MenuAPI.AddElementToLobbyMenu(parent => {
            var repoButton = MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(152f, 60f));
            repoButton.labelTMP.fontSize = 28;
        });
        MenuAPI.AddElementToEscapeMenu(parent => MenuAPI.CreateREPOButton("Mods", CreateModMenu, parent, new Vector2(126f, 65f)));
    }
    
    internal static void CreateModMenu()
    {
        configEntryStore.ChangedEntryValues.Clear();
        
        lastClickedModButton = null;
        
        var repoPopupPage = MenuAPI.CreateREPOPopupPage("Mods", REPOPopupPage.PresetSide.Left, false, true);
        repoPopupPage.scrollView.scrollSpeed = 3f;
        repoPopupPage.maskPadding = repoPopupPage.maskPadding with { top = 35 }; 
        repoPopupPage.onEscapePressed += () => {
            if (hasPopupMenuOpened)
                return false;

            if (configEntryStore.ChangedEntryValues.Count == 0)
                return true;
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?", () => {
                repoPopupPage.ClosePage(true);
                configEntryStore.ChangedEntryValues.Clear();
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
            if (configEntryStore.ChangedEntryValues.Count == 0 || hasPopupMenuOpened)
            {
                repoPopupPage.ClosePage(true);
                return;
            }
            
            MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                () =>
                {
                    repoPopupPage.ClosePage(true);
                    configEntryStore.ChangedEntryValues.Clear();
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
                    
                    if (configEntryStore.ChangedEntryValues.Count == 0)
                    {
                        OpenPage();
                        return;
                    }
                    
                    MenuAPI.OpenPopup("Unsaved Changes", Color.red, "You have unsaved changes, are you sure you want to exit?",
                        () =>
                        {
                            configEntryStore.ChangedEntryValues.Clear();
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
                        modPage.onEscapePressed = () =>  !hasPopupMenuOpened && configEntryStore.ChangedEntryValues.Count == 0;
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Save Changes", () =>
                            {
                                var cachedEntries = configEntryStore.ChangedEntryValues.ToArray();
                                configEntryStore.ChangedEntryValues.Clear();

                                foreach (var (key, value) in cachedEntries)
                                {
                                    key.BoxedValue = value;
                                    configEntryStore.OriginalEntryValues[key] = value;
                                }
                                
                            }, mainPageParent, new Vector2(370f, 18f));
                        });
                        
                        modPage.AddElement(mainPageParent => {
                            MenuAPI.CreateREPOButton("Revert", () =>
                            {
                                if (configEntryStore.ChangedEntryValues.Count == 0)
                                    return;
                                
                                configEntryStore.ChangedEntryValues.Clear();
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

                                    configEntryStore.ChangedEntryValues.Clear();
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

                configEntryStore.OriginalEntryValues.Remove(entry);
                configEntryStore.OriginalEntryValues.Add(entry, entry.BoxedValue);

                if (configEntryStrategies.TryGetValue(entry.SettingType, out var strategy))
                {
                    modPage.AddElementToScrollView(scrollView => strategy.Execute(
                        scrollView,
                        entry,
                        new EntryParameters(modName, configEntryStore)
                    ));

                    continue;
                }

                if (entry.SettingType.IsSubclassOf(typeof(Enum)))
                {
                    HandleEnumEntry(modPage, modName, entry);
                }
            }
            
            modPage.AddElementToScrollView(scrollView => MenuAPI.CreateREPOSpacer(scrollView, size: new Vector2(0, 20)).rectTransform);   
        }
    }

    private static void HandleEnumEntry(REPOPopupPage modPage, string modName, ConfigEntryBase entry)
    {
        var enumType = entry.SettingType;
        var values = Enum.GetNames(enumType);

        modPage.AddElementToScrollView(scrollView =>
        {
            var repoSlider = MenuAPI.CreateREPOSlider(modName, string.Empty, i => //description
            {
                var enumValue = Enum.Parse(enumType, values[i]);

                if (configEntryStore.OriginalEntryValues.TryGetValue(entry, out var originalValue) &&
                    enumValue == originalValue)
                {
                    configEntryStore.ChangedEntryValues.Remove(entry);
                    return;
                }

                configEntryStore.ChangedEntryValues[entry] = enumValue;
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
}