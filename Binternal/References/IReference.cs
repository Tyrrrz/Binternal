namespace Binternal.References;

internal interface IReference
{
    string Id { get; }

    bool IsInternalized { get; }
}
