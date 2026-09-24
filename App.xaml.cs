using System.Windows;
using OmniKeyStudio.Services;
using OmniKeyStudio.ViewModels;

namespace OmniKeyStudio;

public partial class App : Application
{
    private ChatPlaygroundViewModel? _chatPlaygroundVm;

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

        _chatPlaygroundVm = chatPlaygroundVm;

        var mainVm = new MainViewModel(keyManagerVm, chatPlaygroundVm, freeModelsVm, codeVaultVm);

        var mainWindow = new MainWindow
        {
            DataContext = mainVm
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Kapanışta API ile yapılan tüm sohbet, arama ve oturum verilerini bellekten tamamen temizle
        _chatPlaygroundVm?.ClearChatSession();
        base.OnExit(e);
    }
}
