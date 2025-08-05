using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BuildSender
{
    public partial class SenderForm : Form
    {
        private string _file;

        public SenderForm()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 4)
            {
                _file = args[1];
                txtFile.Text = _file;
                txtHost.Text = args[2];
                numPort.Value = int.Parse(args[3]);
                Shown += async (s, e) => await SendAsync(); // auto-send then quit
            }
        }

        private void txtFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _file = openFileDialog1.FileName;
                txtFile.Text = _file;
            }
        }

        private async void btnSend_Click(object sender, EventArgs e) => await SendAsync();

        private async Task SendAsync()
        {
            if (!File.Exists(_file))
            {
                MessageBox.Show("Choose a valid file.");
                return;
            }

            try
            {
                string host = txtHost.Text;
                int port = (int)numPort.Value;

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(host, port);
                    using (var ns = client.GetStream())
                    using (var bw = new BinaryWriter(ns))
                    {
                        byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(_file));
                        bw.Write(nameBytes.Length);
                        bw.Write(nameBytes);
                        long fileLen = new FileInfo(_file).Length;
                        bw.Write(fileLen);

                        using (var fs = new FileStream(_file, FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[64 * 1024];
                            int read;
                            long sent = 0;
                            while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await ns.WriteAsync(buffer, 0, read);
                                sent += read;
                                lblStatus.Text = $"Sent {sent * 100 / fileLen}% …";
                                Application.DoEvents();
                            }
                        }
                    }
                }

                lblStatus.Text = "Done ✔";
                if (Environment.GetCommandLineArgs().Length == 4)
                    Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
            }
        }
    }
}
