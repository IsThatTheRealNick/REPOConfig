using System;
using BepInEx.Configuration;
using MenuLib;
using TMPro;
using UnityEngine;

namespace REPOConfig.Strategies
{
    public class BooleanConfigEntryStrategy : ConfigEntryStrategy<bool>
    {
        protected override RectTransform Execute(Transform scrollView, ConfigEntry<bool> entry, EntryParameters parameters)
        {
            var repoToggle = MenuAPI.CreateREPOToggle(parameters.ModName, b =>
            {
                if (parameters.Store.OriginalEntryValues.TryGetValue(entry, out var originalValue) && b == (bool) originalValue)
                {
                    parameters.Store.ChangedEntryValues.Remove(entry);
                    return;
                }
                                
                parameters.Store.ChangedEntryValues[entry] = b;
            }, scrollView, defaultValue: (bool)entry.BoxedValue);
            repoToggle.labelTMP.fontStyle = FontStyles.Normal;
            return repoToggle.rectTransform;
        }
    }
}