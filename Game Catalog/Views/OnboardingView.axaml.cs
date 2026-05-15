using Avalonia.Controls;
using Game_Catalog.ViewModels;
using System;

namespace Game_Catalog.Views;

public partial class OnboardingWindow : Window
{
    private bool _isSetupFinished = false;

    public OnboardingWindow()
    {
        InitializeComponent();
        var vm = new OnboardingViewModel();
        DataContext = vm;
        vm.Completed += OnCompleted;
    }

    private void OnCompleted()
    {
        _isSetupFinished = true;
        Close();
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (!_isSetupFinished)
        {

            Environment.Exit(0);
        }
    }
}