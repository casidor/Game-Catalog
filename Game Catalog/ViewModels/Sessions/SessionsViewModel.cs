using Game_Catalog.Models;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the play sessions page. </summary>
    public partial class SessionsViewModel : ViewModelBase
    {
        /// <summary> All sessions, newest first. </summary>
        public IEnumerable<PlaySession> Sessions =>
            AppData.Instance.Sessions.OrderByDescending(s => s.StartTime);

        /// <summary> Indicates whether there are no sessions yet. </summary>
        public bool IsEmpty => AppData.Instance.Sessions.Count == 0;

        public SessionsViewModel()
        {
            AppData.Instance.Sessions.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Sessions));
                OnPropertyChanged(nameof(IsEmpty));
            };
        }
    }
}