using Avalonia;
using Avalonia.Controls;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views
{
    public partial class PlatformsTabView : UserControl
    {
        /// <summary> Header clicks and sort arrows of the table. </summary>
        private readonly DataGridSortHeaders _sortHeaders;

        public PlatformsTabView()
        {
            InitializeComponent();

            _sortHeaders = new DataGridSortHeaders(PlatformsGrid,
                () => DataContext is PlatformsViewModel vm
                    ? ((object?)vm.SortField, vm.SortDescending)
                    : ((object?)null, false),
                field =>
                {
                    if (DataContext is PlatformsViewModel vm && field is DictionarySortField sortField)
                        vm.SortByColumn(sortField);
                });
        }

        /// <summary> The tab gets its ViewModel through a binding, so the headers follow whichever one is set. </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            _sortHeaders?.Attach(DataContext);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _sortHeaders.Attach(DataContext);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _sortHeaders.Detach();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
