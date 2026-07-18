namespace REPOConfig.Strategies
{
    public sealed record EntryParameters(string ModName, ConfigEntryStore Store)
    {
        public string ModName { get; } = ModName;
        public ConfigEntryStore Store { get; } = Store;
    }
}
