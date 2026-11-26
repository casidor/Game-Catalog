using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> What the top bar of the dictionaries page needs from the selected tab: search and adding. </summary>
    public interface IDictionaryTab : INotifyPropertyChanged
    {
        string SearchText { get; set; }
        string SearchSummary { get; }
        string SearchPlaceholder { get; }
        string AddTip { get; }
        IAsyncRelayCommand AddCommand { get; }
    }
}
