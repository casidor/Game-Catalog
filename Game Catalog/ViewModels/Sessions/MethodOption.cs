using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> An entry of the entry-method dropdown in the filter panel. A null value means "any method". </summary>
    public sealed record MethodOption(string Name, EntryMethod? Value);
}
