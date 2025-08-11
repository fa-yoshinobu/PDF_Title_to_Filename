using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PdfTitleRenamer.Services;
using PdfTitleRenamer.ViewModels;
using System.Windows;
using System;
using System.Windows.Threading;

namespace PdfTitleRenamer
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public App()
        {
            // ハンドルされていない例外をキャッチ
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                _serviceProvider = serviceCollection.BuildServiceProvider();

                var mainWindow = _serviceProvider.GetRequiredService<Views.MainWindow>();
                
                // ウィンドウを確実に表示
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.ShowInTaskbar = true;
                
                mainWindow.Show();
                
                // ウィンドウをアクティブにする
                mainWindow.Activate();
                mainWindow.Focus();
            }
            catch (Exception ex)
            {
                // ユーザーにエラーを表示
                MessageBox.Show($"アプリケーションの起動に失敗しました。\n\n" +
                              $"エラー: {ex.Message}", 
                              "起動エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                // アプリケーションを終了
                Environment.Exit(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging (Microsoft.Extensions.Logging) - 本番環境では最小限
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // カスタムログサービス
            services.AddSingleton<ILogService, LogService>();

            // PDF処理サービス
            services.AddSingleton<IPdfProcessingService, PdfProcessingService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SettingsWindowViewModel>();

            // Views
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.SettingsWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show($"アプリケーションでエラーが発生しました。\n\n" +
                              $"エラー: {e.Exception.Message}", 
                              "アプリケーションエラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                e.Handled = true; // アプリケーションを継続
            }
            catch
            {
                // 何もできない場合はアプリケーションを終了
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 致命的エラーの場合は静かに終了
        }
    }
}
