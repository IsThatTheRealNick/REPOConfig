using System.Collections.Generic;
using BepInEx.Configuration;

namespace REPOConfig
{
    public class ConfigEntryStore
    {
        internal Dictionary<ConfigEntryBase, object> ChangedEntryValues { get; } = [];
        internal Dictionary<ConfigEntryBase, object> OriginalEntryValues { get; } = [];
    }
}