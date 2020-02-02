using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace Encryptor
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

        private void OnBrowseApplicationClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Application files (*.exe)|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                CheckPathExists = true,
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == true)
            {
                tbApplicationPath.Text = dialog.FileName;
            }
        }

        private void OnEncryptClick(object sender, RoutedEventArgs e)
        {
            if(File.Exists(tbApplicationPath.Text)
                && !string.IsNullOrEmpty(tbPassword.Password))
            {
                bEncrypt.IsEnabled = false;

                EncriptSelectedFile(tbApplicationPath.Text, tbPassword.Password);

                File.Delete(tbApplicationPath.Text);

                MessageBox.Show("Application is encrypted");

                bEncrypt.IsEnabled = true;
            }
        }

        private void EncriptSelectedFile(string applicationPath, string password)
        {
            using var keyGenerator = new Rfc2898DeriveBytes(
                                password,
                                _salt,
                                1000,
                                HashAlgorithmName.SHA256);

            using var encryptionAlgorithm = Rijndael.Create();
            encryptionAlgorithm.BlockSize = 128;

            var key = keyGenerator.GetBytes(encryptionAlgorithm.KeySize / 8);

            using var encryptor = encryptionAlgorithm.CreateEncryptor(key, _iv);

            using var inputFileStream = new FileStream(
                                applicationPath,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read
                                );

            using var outputFileStream = new FileStream(
              applicationPath + ".enc",
              FileMode.Create,
              FileAccess.Write
            );

            using var ecnryptionStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);

            var buffer = new byte[1024];

            while (true)
            {
                int bytesRead = inputFileStream.Read(buffer, 0, 1024);
                if (bytesRead == 0)
                    break;

                ecnryptionStream.Write(buffer, 0, bytesRead);
            }
        }
    }
}
