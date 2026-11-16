using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> One button of the navigation sidebar. </summary>
    public partial class NavItem : ObservableObject
    {
        /// <summary> Icon shown in both sidebar modes. </summary>
        public string Icon { get; }

        /// <summary> Caption shown in the wide mode and as a tooltip. </summary>
        public string Title { get; }

        /// <summary> Page opened by the button. </summary>
        public ViewModelBase Page { get; }

        /// <summary> True when this section is the one the user is in. </summary>
        [ObservableProperty] private bool _isActive;

        public NavItem(string icon, string title, ViewModelBase page)
        {
            Icon = icon;
            Title = title;
            Page = page;
        }
    }
}
