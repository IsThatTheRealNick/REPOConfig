using System;

namespace REPOConfig;
 
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property), Obsolete("This attribute is not used and will be removed eventually.")]
public class REPOConfigEntryAttribute : Attribute

{
    public REPOConfigEntryAttribute(string displayName) {}
    
    public REPOConfigEntryAttribute(string displayName, int min, int max, string prefix = "", string postfix = "") { }
    
    public REPOConfigEntryAttribute(string displayName, float min, float max, string prefix = "", string postfix = "") {}
    
    public REPOConfigEntryAttribute(string displayName, float min, float max, int precision = 2, string prefix = "", string postfix = "") { }
    
    public REPOConfigEntryAttribute(string displayName, string prefix = "", string postfix = "") { }
    
    public REPOConfigEntryAttribute(string displayName, params string[] customOptions) { }
}