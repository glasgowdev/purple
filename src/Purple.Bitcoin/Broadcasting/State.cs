namespace Purple.Bitcoin.Broadcasting
{
    public enum State
    {
        CantBroadcast,
        ToBroadcast,
        Broadcasted,
        Propagated
    }
}
