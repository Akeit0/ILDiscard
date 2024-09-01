using System;

namespace ILDiscard
{
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property|AttributeTargets.Method|AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Interface|AttributeTargets.Enum)]
    public class DiscardAttribute:Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property|AttributeTargets.Method)]
    public class DontDiscardAttribute:Attribute
    {
    }
    
    
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Interface|AttributeTargets.Enum)]
    public class DiscardMembersAttribute:Attribute
    {
        public DiscardMembersAttribute(DiscardMembersOptions options=DiscardMembersOptions.None)
        {
        }
        
    }
    [Flags]
    public enum DiscardMembersOptions
    {
        None=0,
        DiscardByDefault=1
    }
    
    
    
}