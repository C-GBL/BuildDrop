using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BuildReceiver
{
    public partial class ReceiverForm : Form
    {
        private TcpListener _listener;
        private string _destFolder;

        public ReceiverForm() => InitializeComponent();

        private void txtFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                _destFolder = folderBrowserDialog1.SelectedPath;
                txtFolder.Text = _destFolder;
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_destFolder))
            {
                MessageBox.Show("Choose a destination folder first.");
                return;
            }

            int port = (int)numPort.Value;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            btnStart.Enabled = false;
            lblStatus.Text = $"Listening on *:{port}…";

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var ns = client.GetStream())
            using (var br = new BinaryReader(ns))
            {
                try
                {
                    int nameLen = br.ReadInt32();
                    string fileName = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLen));
                    long fileSize = br.ReadInt64();

                    string destPath = Path.Combine(_destFolder, fileName);
                    using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[64 * 1024];
                        long readTotal = 0;
                        int read;
                        while (readTotal < fileSize &&
                               (read = await ns.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, read);
                            readTotal += read;
                        }
                    }

                    Invoke((Action)(() =>
                        lblStatus.Text = $"Received {fileName} ({fileSize / 1024} KB)"));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() => lblStatus.Text = $"Error: {ex.Message}"));
                }
            }
        }
    }
}
