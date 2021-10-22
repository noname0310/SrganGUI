using System.Windows.Forms;
using System.Diagnostics;

namespace SrganGUI
{
    public partial class Form1 : Form
    {
        private static readonly string realesrganCmd = "-i \"{0}\" -o \"{1}\"";
        private static readonly string realesrnetCmd = "-i \"{0}\" -o \"{1}\" -n realesrnet-x4plus";
        private static readonly string esrganCmd = "-i \"{0}\" -o \"{1}\" -n esrgan-x4";
        private readonly List<Process> processList = new();

        public Form1()
        {
            InitializeComponent();
            comboBox.SelectedIndex = 0;
            textBox.AllowDrop = true;
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void TextBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] filePathArray)
                return;

            string command;
            string appendText;
            switch (comboBox.SelectedIndex)
            {
                case 0:
                    command = realesrganCmd;
                    appendText = "-realesrgan";
                    break;
                case 1:
                    command = realesrnetCmd;
                    appendText = "-realesrgnet";
                    break;
                case 2:
                    command = esrganCmd;
                    appendText = "-esrgan";
                    break;
                default:
                    goto case 0;
            }

            foreach (var item in processList)
            {
                try
                {
                    item.Kill();
                    item.Dispose();
                }
                catch (InvalidOperationException) { }
            }
            processList.Clear();
            textBox.Clear();
            foreach (var path in filePathArray)
            {
                var dir = Path.GetDirectoryName(path) ?? throw new Exception();
                var fileName = Path.GetFileNameWithoutExtension(path);
                var fileExt = Path.GetExtension(path);
                var newPath = Path.Combine(dir, $"{fileName}{appendText}{fileExt}");
                var process = new Process()
                {
                    StartInfo = new()
                    {
                        FileName = "realesrgan-ncnn-vulkan.exe",
                        Arguments = string.Format(command, path, newPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                    SynchronizingObject = this,
                    EnableRaisingEvents = true,
                };
                process.Exited += (o, _) => {
                    var p = (Process?)o;
                    textBox.AppendText($"[{p?.Id}]task exited: {p?.ExitCode} \r\n");
                    process?.Dispose();
                };
                process.OutputDataReceived += (o, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        textBox.AppendText($"[{((Process)o).Id}]{e.Data}\r\n");
                };
                process.ErrorDataReceived += (o, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        textBox.AppendText($"[{((Process)o).Id}]{e.Data}\r\n");
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                processList.Add(process);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var item in processList)
            {
                try
                {
                    item.StandardInput.Close();
                    item.Kill();
                    item.Dispose();
                }
                catch (InvalidOperationException) { }
            }
        }
    }
}
