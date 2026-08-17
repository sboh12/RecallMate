using System.Windows;
using RecallMate.Services;
using RecallMate.ViewModels;

namespace RecallMate;

public partial class MainWindow : Window
{
    private readonly WindowCaptureService _captureService = new(TimeSpan.FromSeconds(5));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new SqliteDataService(), _captureService);
        Closed += (_, _) => _captureService.Dispose();
    }
}
