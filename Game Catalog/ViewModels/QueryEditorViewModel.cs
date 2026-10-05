using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the developer SQL query editor. </summary>
    public partial class QueryEditorViewModel : ViewModelBase
    {
        private const string DefaultQuery = "SELECT ";

        /// <summary>SQL text entered by the user.</summary>
        [ObservableProperty]
        private string _queryText = DefaultQuery;

        /// <summary>When true, modifying statements are allowed (read-write connection).</summary>
        [ObservableProperty]
        private bool _allowModify;

        /// <summary>Error or status message shown under the editor.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMessage))]
        private string _message = string.Empty;

        /// <summary>True if there is a message to show.</summary>
        public bool HasMessage => !string.IsNullOrEmpty(Message);

        /// <summary>Column names of the last result.</summary>
        public IReadOnlyList<string> Columns { get; private set; } = Array.Empty<string>();

        /// <summary>Rows of the last result.</summary>
        public IReadOnlyList<string?[]> Rows { get; private set; } = Array.Empty<string?[]>();

        /// <summary>Raised when a new result is ready so the view can rebuild grid columns.</summary>
        public event Action? ResultChanged;

        /// <summary>Executes the entered SQL.</summary>
        [RelayCommand]
        private void Execute()
        {
            if (string.IsNullOrWhiteSpace(QueryText)) return;

            try
            {
                var result = DatabaseService.ExecuteQuery(QueryText, AllowModify);
                Columns = result.Columns;
                Rows = result.Rows;
                Message = result.Columns.Count > 0
                    ? $"Рядків: {result.Rows.Count}"
                    : $"Виконано. Змінено рядків: {result.RowsAffected}. " +
                    "Перезапустіть програму, щоб побачити зміни в інтерфейсі.";
            }
            catch (SqliteException ex)
            {
                Columns = Array.Empty<string>();
                Rows = Array.Empty<string?[]>();
                Message = $"Помилка: {ex.Message}";
            }
            ResultChanged?.Invoke();
        }

        /// <summary>Resets the editor text and the result.</summary>
        [RelayCommand]
        private void Clear()
        {
            QueryText = DefaultQuery;
            Columns = Array.Empty<string>();
            Rows = Array.Empty<string?[]>();
            Message = string.Empty;
            ResultChanged?.Invoke();
        }
    }
}