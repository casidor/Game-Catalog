using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the studio management page.
    /// </summary>
    public partial class StudioViewModel : ViewModelBase
    {
        /// <summary>
        /// Collection of all studios in the library.
        /// </summary>
        public ObservableCollection<Studio> Studios => AppData.Instance.Studios;
        /// <summary>
        /// Currently selected studio in the list.
        /// </summary>
        [ObservableProperty]
        private Studio? _selectedStudio;
    }
}
