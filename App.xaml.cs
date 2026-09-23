using System.Windows;
using OmniKeyStudio.Services;
using OmniKeyStudio.ViewModels;

namespace OmniKeyStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Core Services
        var detectorService = new KeyDetectorService();
        var validatorService = new KeyValidatorService();
        var storageService = new SecureStorageService();
        var llmService = new LlmClientService();
        var codeGenService = new CodeGeneratorService();

        // ViewModels
        var keyManagerVm = new KeyManagerViewModel(detectorService, validatorService, storageService);
        var chatPlaygroundVm = new ChatPlaygroundViewModel(llmService, storageService);
        var freeModelsVm = new FreeModelsViewModel(llmService, storageService);
        var codeVaultVm = new CodeVaultViewModel(storageService, codeGenService);

        var mainVm = new MainViewModel(keyManagerVm, chatPlaygroundVm, freeModelsVm, codeVaultVm);

        var mainWindow = new MainWindow
        {
            DataContext = mainVm
        };

        mainWindow.Show();
    }
}
