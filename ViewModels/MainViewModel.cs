using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniKeyStudio.Services;

namespace OmniKeyStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public KeyManagerViewModel KeyManager { get; }
    public ChatPlaygroundViewModel ChatPlayground { get; }
    public FreeModelsViewModel FreeModels { get; }
    public CodeVaultViewModel CodeVault { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _globalStatus = "OmniKey AI Studio Hazır • Tüm API Anahtarlarınız Güvenli DPAPI Kasasında Saklanır";

    public MainViewModel(
        KeyManagerViewModel keyManager,
        ChatPlaygroundViewModel chatPlayground,
        FreeModelsViewModel freeModels,
        CodeVaultViewModel codeVault)
    {
        KeyManager = keyManager;
        ChatPlayground = chatPlayground;
        FreeModels = freeModels;
        CodeVault = codeVault;

        // When keys change, refresh chat models
        KeyManager.OnKeysChanged += () =>
        {
            ChatPlayground.RefreshModels();
            GlobalStatus = "API anahtarları güncellendi ve modeller yenilendi.";
        };

        // When free model is selected for chat
        FreeModels.OnSelectModelForChat += (provider, modelId) =>
        {
            SelectedTabIndex = 1; // Switch to Chat tab
            var target = ChatPlayground.AvailableModels.FirstOrDefault(m => m.ModelId == modelId && m.Provider == provider);
            if (target != null)
            {
                ChatPlayground.SelectedModel = target;
                GlobalStatus = $"Sohbet için '{target.DisplayName}' modeli seçildi.";
            }
        };
    }

    [RelayCommand]
    private void SwitchTab(int index)
    {
        SelectedTabIndex = index;
    }
}
