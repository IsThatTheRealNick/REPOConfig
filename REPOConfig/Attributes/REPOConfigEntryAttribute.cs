using System;

namespace REPOConfig;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property), Obsolete("This attribute won't do anything while being reworked. ")]
public class REPOConfigEntryAttribute : Attribute
{
    internal readonly string displayName;
    internal readonly float? min, max;
    internal readonly int precision;
    internal readonly string prefix, postfix;
    internal readonly string[] customOptions;
    
    /// <summary>
    /// Applies a custom name to this entry
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    public REPOConfigEntryAttribute(string displayName) => this.displayName = displayName;
    
    /// <summary>
    /// Applies a custom name to this entry while providing a set range, prefix, and/or postfix
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    /// <param name="min">The minimum value the slider as an integer</param>
    /// <param name="max">The maximum value of the slider as an integer</param>
    /// <param name="prefix">Text that comes before the value (i.e. $1)</param>
    /// <param name="postfix">Text that comes after the value (i.e. 50 mph)</param>
    public REPOConfigEntryAttribute(string displayName, int min, int max, string prefix = "", string postfix = "")
    {
        this.displayName = displayName;
        this.min = min;
        this.max = max;
        this.prefix = prefix;
        this.postfix = postfix;
    }
    
    /// <summary>
    /// Applies a custom name to this entry while providing a set range, prefix, and/or postfix
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    /// <param name="min">The minimum value the slider as a floating point number</param>
    /// <param name="max">The maximum value of the slider as a floating point number</param>
    /// <param name="prefix">Text that comes before the value (i.e. $1)</param>
    /// <param name="postfix">Text that comes after the value (i.e. 50 mph)</param>
    public REPOConfigEntryAttribute(string displayName, float min, float max, string prefix = "", string postfix = "")
    {
        this.displayName = displayName;
        this.min = min;
        this.max = max;
        this.prefix = prefix;
        this.postfix = postfix;
    }
    
    /// <summary>
    /// Applies a custom name to this entry while providing a set range, precision, prefix, and/or postfix
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    /// <param name="min">The minimum value the slider as a floating point number</param>
    /// <param name="max">The maximum value of the slider as a floating point number</param>
    /// <param name="precision">The number of decimal points to keep (2 by default)</param>
    /// <param name="prefix">Text that comes before the value (i.e. $1)</param>
    /// <param name="postfix">Text that comes after the value (i.e. 50 mph)</param>
    public REPOConfigEntryAttribute(string displayName, float min, float max, int precision = 2, string prefix = "", string postfix = "")
    {
        this.displayName = displayName;
        this.min = min;
        this.max = max;
        this.precision = precision;
        this.prefix = prefix;
        this.postfix = postfix;
    }
    
    /// <summary>
    /// Applies a custom name to this entry while providing a prefix and/or postfix
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    /// <param name="prefix">Text that comes before the value (i.e. $1)</param>
    /// <param name="postfix">Text that comes after the value (i.e. 50 mph)</param>
    public REPOConfigEntryAttribute(string displayName, string prefix = "", string postfix = "")
    {
        this.displayName = displayName;
        this.prefix = prefix;
        this.postfix = postfix;
    }
    
    /// <summary>
    /// Applies a custom name to this entry while providing names instead of values
    /// </summary>
    /// <param name="displayName">A custom name that shows up in the menu</param>
    /// <param name="customOptions">Names that will align with it's zero-based index (i.e. "Slow" (The int will be = 0), "Fast" (Will be = 1), "Super Fast" (Will be = 2)) </param>
    public REPOConfigEntryAttribute(string displayName, params string[] customOptions)
    {
        this.displayName = displayName;
        this.customOptions = customOptions;
    }
}