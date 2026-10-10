using System;
using System.Linq;
using System.Reflection;

// Does an attribute on the implementing half of a partial property land on the compiled property?
PropertyInfo p = typeof(Holder).GetProperty(nameof(Holder.Name))!;
Console.WriteLine("Attributes on Holder.Name: " + string.Join(", ", p.GetCustomAttributes().Select(a => a.GetType().Name)));

[AttributeUsage(AttributeTargets.Property)] sealed class DeclaredByAuthorAttribute : Attribute { }
[AttributeUsage(AttributeTargets.Property)] sealed class AddedByGeneratorAttribute : Attribute { }

// What the author writes.
partial class Holder
{
    [DeclaredByAuthor] public partial string Name { get; set; }
}

// What a generator would emit.
partial class Holder
{
    private string _name = "";
    [AddedByGenerator] public partial string Name { get => _name; set => _name = value; }
}
