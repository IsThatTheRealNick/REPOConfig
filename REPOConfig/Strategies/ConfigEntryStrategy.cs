using System;
using BepInEx.Configuration;
using UnityEngine;

namespace REPOConfig.Strategies
{
    public abstract class ConfigEntryStrategy<T> : IConfigEntryStrategy
    {
        public Type TargetType { get; } = typeof(T);
        
        public RectTransform Execute(Transform scrollView, ConfigEntryBase entry, EntryParameters parameters)
        {
            return Execute(scrollView, (ConfigEntry<T>)entry, parameters);
        }
        
        protected abstract RectTransform Execute(Transform scrollView, ConfigEntry<T> entry, EntryParameters parameters);
    }
    
    internal interface IConfigEntryStrategy
    {
        Type TargetType { get; }

        RectTransform Execute(Transform scrollView, ConfigEntryBase entry, EntryParameters parameters);
    }
}