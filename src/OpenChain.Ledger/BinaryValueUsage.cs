namespace OpenChain.Ledger
{
    public enum BinaryValueUsage : int
    {
        Default         = 0,
        Text            = 1,
        Int64           = 2,

        AccountKey      = 0 + 256,
        AssetDefinition = 1 + 256,
        Alias           = 2 + 256
    }
}
