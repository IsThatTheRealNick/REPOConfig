using System;
using BepInEx.Configuration;
using MenuLib;
using TMPro;
using UnityEngine;

namespace REPOConfig.Strategies
{
    public class StringConfigEntryStrategy : ConfigEntryStrategy<string>
    {
        protected override RectTransform Execute(Transform scrollView, ConfigEntry<string> entry, EntryParameters parameters)
        {
            return entry.Description.AcceptableValues is AcceptableValueList<string> acceptableValueList
                ? HandleListEntry(scrollView, entry, parameters, acceptableValueList)
                : HandleStringEntry(scrollView, entry, parameters);
        }

        private RectTransform HandleStringEntry(Transform scrollView, ConfigEntry<string> entry, EntryParameters parameters)
        {
            var defaultValue = (string) entry.DefaultValue;
                            
            var repoInputField = MenuAPI.CreateREPOInputField(parameters.ModName, s =>
            {
                if (parameters.Store.OriginalEntryValues.TryGetValue(entry, out var originalValue) && s == (string) originalValue)
                {
                    parameters.Store.ChangedEntryValues.Remove(entry);
                    return;
                }
                                
                parameters.Store.ChangedEntryValues[entry] = s;
            }, scrollView, Vector2.zero, false, !string.IsNullOrEmpty(defaultValue) ? defaultValue : "<NONE>", (string) entry.BoxedValue);
            repoInputField.labelTMP.fontStyle = repoInputField.inputStringSystem.inputTMP.fontStyle = FontStyles.Normal;
                            
            return repoInputField.rectTransform;
        }
        
        private RectTransform HandleListEntry(Transform scrollView, ConfigEntry<string> entry, EntryParameters parameters, AcceptableValueList<string> acceptableValueList)
        {
            var repoSlider = MenuAPI.CreateREPOSlider(parameters.ModName, string.Empty, s => //description
            {
                if (parameters.Store.OriginalEntryValues.TryGetValue(entry, out var originalValue) && s == (string) originalValue)
                {
                    parameters.Store.ChangedEntryValues.Remove(entry);
                    return;
                }
                                
                parameters.Store.ChangedEntryValues[entry] = s;
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
        }
    }
}