using System;
using BepInEx.Configuration;
using MenuLib;
using TMPro;
using UnityEngine;

namespace REPOConfig.Strategies
{
    public class IntConfigEntryStrategy : ConfigEntryStrategy<int>
    {
        protected override RectTransform Execute(Transform scrollView, ConfigEntry<int> entry, EntryParameters parameters)
        {
            int min;
            int max;

            if (entry.Description.AcceptableValues is AcceptableValueRange<int> acceptableValueRange)
            {
                min = acceptableValueRange.MinValue;
                max = acceptableValueRange.MaxValue;
            }
            else
            {
                var absoluteDefaultValue = Math.Abs((int)entry.BoxedValue);

                min = absoluteDefaultValue switch
                {
                    0 => -(max = 100),
                    <= 100 => -(max = absoluteDefaultValue * 3),
                    _ => -(max = absoluteDefaultValue * 2)
                };
            }

            var repoSlider = MenuAPI.CreateREPOSlider(parameters.ModName, string.Empty, i => //description
            {
                if (parameters.Store.OriginalEntryValues.TryGetValue(entry, out var originalValue) &&
                    i == (int)originalValue)
                {
                    parameters.Store.ChangedEntryValues.Remove(entry);
                    return;
                }

                parameters.Store.ChangedEntryValues[entry] = i;
            }, scrollView, defaultValue: (int)entry.BoxedValue, min: min, max: max);
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
        }
    }
}