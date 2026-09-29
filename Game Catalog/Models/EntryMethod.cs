namespace Game_Catalog.Models
{
    /// <summary> Indicates how a play session record was added to the system. </summary>
    public enum EntryMethod
    {
        /// <summary> Added manually by the user. </summary>
        Manual,

        /// <summary> Detected automatically via process tracking. </summary>
        Auto
    }
}