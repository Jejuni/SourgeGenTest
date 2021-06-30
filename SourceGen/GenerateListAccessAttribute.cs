using System;

namespace SourceGen
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class GenerateListAccessAttribute : Attribute
    {
        public GenerateListAccessAttribute()
        {

        }
        public bool Protected { get; set; }


        public static string AttributeName { get; } = $"SourceGen.{nameof(GenerateListAccessAttribute)}";
        public static string SourceText { get; } = @"
using System;

namespace SourceGen 
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateListAccessAttribute : Attribute
    {
        public GenerateListAccessAttribute()
        {

        }
        public bool Protected { get; set; }
    }
}
";
    }
}
