using System;
using BepInEx.Configuration;
using MenuLib;
using REPOConfig.Extensions;
using TMPro;
using UnityEngine;

namespace REPOConfig.Strategies
{
    public class FloatConfigEntryStrategy : ConfigEntryStrategy<float>
    {
        protected override RectTransform Execute(Transform scrollView, ConfigEntry<float> entry, EntryParameters parameters)
        {
            float min, max;
            var precision = 2;

            if (entry.Description.AcceptableValues is AcceptableValueRange<float> acceptableValueRange)
            {
                min = acceptableValueRange.MinValue;
                max = acceptableValueRange.MaxValue;

                precision = Mathf.Max(min.GetDecimalPlaces(), max.GetDecimalPlaces(),
                    ((float)entry.DefaultValue).GetDecimalPlaces(), 2);
            }
            else
            {
                var absoluteDefaultValue = Math.Abs((float)entry.BoxedValue);

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

            var repoSlider = MenuAPI.CreateREPOSlider(parameters.ModName, string.Empty, f => //description
            {
                if (parameters.Store.OriginalEntryValues.TryGetValue(entry, out var originalValue) &&
                    Math.Abs(f - (float)originalValue) < float.Epsilon)
                {
                    parameters.Store.ChangedEntryValues.Remove(entry);
                    return;
                }

                parameters.Store.ChangedEntryValues[entry] = f;
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
        }
    }
}