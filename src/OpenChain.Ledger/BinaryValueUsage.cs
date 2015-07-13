namespace OpenChain.Ledger
{
    public enum BinaryValueUsage : int
    {
        Text            = 0,
        Int64           = 1,

        AccountKey      = 0 + 256,
        AssetDefinition = 1 + 256,
    }
}
