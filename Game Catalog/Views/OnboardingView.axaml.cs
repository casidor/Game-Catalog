using Avalonia.Controls;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow()
    {
        InitializeComponent();
        var vm = new OnboardingViewModel();
        DataContext = vm;
        vm.Completed += OnCompleted;
    }

    private void OnCompleted()
    {
        Close();
    }
}