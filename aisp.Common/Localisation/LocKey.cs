namespace aisp.Common.Localisation;

public readonly record struct LocKey(string Value)
{
    public override string ToString() => Value;

    public static implicit operator string(LocKey key) => key.Value;
}
