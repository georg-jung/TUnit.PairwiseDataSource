#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Compatibility shim for .NET Standard 2.0, where <c>ModuleInitializerAttribute</c> is not
/// defined but is required by TUnit-generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class ModuleInitializerAttribute : Attribute
{
}
#endif
