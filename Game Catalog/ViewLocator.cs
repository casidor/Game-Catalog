using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Game_Catalog.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Game_Catalog
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// </summary>
    [RequiresUnreferencedCode(
        "Default implementation of ViewLocator involves reflection which may be trimmed away.",
        Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var viewName = param.GetType().FullName!
                .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
                .Replace("ViewModel", "View", StringComparison.Ordinal);

            var type = Type.GetType(viewName)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(viewName))
                    .FirstOrDefault(t => t != null);

            if (type != null)
                return (Control)Activator.CreateInstance(type)!;

            return new TextBlock { Text = "Not Found: " + viewName };
        }

        public bool Match(object? data) => data is ViewModelBase;
    }
}
