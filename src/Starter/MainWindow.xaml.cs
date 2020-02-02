using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;

namespace Starter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly byte[] _salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly byte[] _iv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        private readonly string _encryptedApplicationPath;

        public MainWindow()
        {
            InitializeComponent();

            _encryptedApplicationPath = GetEncryptedApplicationPath();

            if(string.IsNullOrEmpty(_encryptedApplicationPath))
            {
                MessageBox.Show("Unable to find single file to start");
                Application.Current.Shutdown();
            }
        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Password))
                return;

            var applicationPath = _encryptedApplicationPath[0..^4];

            Hide();

            if (File.Exists(applicationPath))
            {
                Process.Start(applicationPath);
                Application.Current.Shutdown();
                return;
            }

            DecryptApplication(applicationPath);

            StartApplicationAndWaitForExit(applicationPath);

            DeleteApplication(applicationPath);

            Application.Current.Shutdown();
        }

        private string GetEncryptedApplicationPath()
        {
            var encryptedApplicationFiles = Directory.GetFiles(
                Environment.CurrentDirectory,
                "*.exe.enc"
                );

            if(encryptedApplicationFiles.Length == 1)
            {
                return encryptedApplicationFiles[0];
            }

            encryptedApplicationFiles = Directory.GetFiles(
                Path.GetDirectoryName(GetType().Assembly.Location),
                "*.exe.enc"
                );

            if (encryptedApplicationFiles.Length == 1)
            {
                return encryptedApplicationFiles[0];
            }

            return null;
        }

        private void DecryptApplication(string applicationPath)
        {
            using var keyGenerator = new Rfc2898DeriveBytes(
                tbPassword.Password,
                _salt,
                1000,
                HashAlgorithmName.SHA256
            );

            using var encryptionAlgorithm = Rijndael.Create();

            encryptionAlgorithm.BlockSize = 128;

            var key = keyGenerator.GetBytes(encryptionAlgorithm.KeySize / 8);

            var decryptor = encryptionAlgorithm.CreateDecryptor(key, _iv);

            using var inputFileStream = new FileStream(
                _encryptedApplicationPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
                );

            using var decryptionStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read);

            using (var outputFileStream = new FileStream(
                    applicationPath,
                    FileMode.Create,
                    FileAccess.Write
                    ))
            {
                var buffer = new byte[1024];

                while (true)
                {
                    int bytesRead = decryptionStream.Read(buffer, 0, 1024);
                    if (bytesRead == 0)
                        break;

                    outputFileStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        private void StartApplicationAndWaitForExit(string applicationPath)
        {
            var startInfo = new ProcessStartInfo(applicationPath);

            var process = Process.Start(startInfo);

            process.WaitForExit();
        }

        private void DeleteApplication(string applicationPath)
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                try
                {
                    File.Delete(applicationPath);
                    return;
                }
                catch
                {

                }
            }

            MessageBox.Show("Unable to delete application. Please, do it manually");
        }
    }
}
