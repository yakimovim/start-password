using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Password))
                return;

            var encryptedApplicationFiles = Directory.GetFiles(
                Environment.CurrentDirectory,
                "*.exe.enc"
                );

            if(encryptedApplicationFiles.Length != 1)
            {
                MessageBox.Show("Unable to find single file to start");
                Application.Current.Shutdown();
            }

            var encryptedApplicationPath = encryptedApplicationFiles[0];

            var applicationPath = encryptedApplicationPath[0..^4];

            if (File.Exists(applicationPath))
            {
                Process.Start(applicationPath);
                Application.Current.Shutdown();
            }

            bStart.IsEnabled = false;

            using var keyGenerator = new Rfc2898DeriveBytes(
                tbPassword.Password, 
                _salt,
                1000,
                HashAlgorithmName.SHA256);

            using var encryptionAlgorithm = Rijndael.Create();

            encryptionAlgorithm.BlockSize = 128;

            var key = keyGenerator.GetBytes(encryptionAlgorithm.KeySize / 8);

            var decryptor = encryptionAlgorithm.CreateDecryptor(key, _iv);

            using var inputFileStream = new FileStream(
                encryptedApplicationPath,
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

            var startInfo = new ProcessStartInfo(applicationPath);

            var process = Process.Start(startInfo);

            Hide();

            process.WaitForExit();

            File.Delete(applicationPath);

            Application.Current.Shutdown();

            bStart.IsEnabled = true;
        }
    }
}
