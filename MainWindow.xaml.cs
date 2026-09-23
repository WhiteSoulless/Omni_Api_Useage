using System.ComponentModel;
using System.Windows;
using OmniKeyStudio.ViewModels;

namespace OmniKeyStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            SyncRadioButtons(vm.SelectedTabIndex);
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedTabIndex) && DataContext is MainViewModel vm)
        {
            SyncRadioButtons(vm.SelectedTabIndex);
        }
    }

    private void SyncRadioButtons(int index)
    {
        TabBtnKeys.IsChecked = (index == 0);
        TabBtnChat.IsChecked = (index == 1);
        TabBtnFreeModels.IsChecked = (index == 2);
        TabBtnCodeVault.IsChecked = (index == 3);
    }

    private void NavKeyManager_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectedTabIndex = 0;
    }

    private void NavChat_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectedTabIndex = 1;
    }

    private void NavFreeModels_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectedTabIndex = 2;
    }

    private void NavCodeVault_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SelectedTabIndex = 3;
    }
}