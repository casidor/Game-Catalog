using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    public abstract class ViewModelBase : ObservableValidator
    {
        /// <summary> Called when the page is shown. Pages override it to load their data. </summary>
        public virtual void OnNavigatedTo() { }
    }
}
