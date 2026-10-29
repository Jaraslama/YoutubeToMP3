using System.Text;

namespace YoutubeToMP3
{
    internal sealed class FamilyDownloaderForm : Form
    {
        private readonly TextBox urlBox = new();
        private readonly ListBox urlListBox = new();
        private readonly Button addButton = new();
        private readonly Button removeButton = new();
        private readonly Button clearButton = new();
        private readonly Button downloadButton = new();
        private readonly Label statusLabel = new();
        private readonly ProgressBar progressBar = new();
        private readonly Downloads downloads = new();

        public FamilyDownloaderForm()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            Text = "Rodinný stahovač hudby";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(620, 480);
            ClientSize = new Size(760, 580);
            Font = new Font("Segoe UI", 10F);
            AllowDrop = true;
            DragEnter += HandleDragEnter;
            DragDrop += HandleDrop;

            var title = new Label
            {
                Text = "Připravte si seznam skladeb",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 22)
            };
            Controls.Add(title);

            var dropHint = new Label
            {
                Text = "Vložte odkaz na YouTube / YouTube Music nebo ho sem přetáhněte:",
                AutoSize = true,
                Location = new Point(24, 64)
            };
            Controls.Add(dropHint);

            urlBox.Location = new Point(24, 92);
            urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            urlBox.Width = ClientSize.Width - 150;
            urlBox.AllowDrop = true;
            urlBox.DragEnter += HandleDragEnter;
            urlBox.DragDrop += HandleDrop;
            urlBox.KeyDown += UrlBox_KeyDown;
            Controls.Add(urlBox);

            addButton.Text = "Přidat";
            addButton.Location = new Point(urlBox.Right + 10, 90);
            addButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addButton.Size = new Size(92, 30);
            addButton.Click += (_, _) => AddUrl(urlBox.Text);
            Controls.Add(addButton);

            var queueLabel = new Label
            {
                Text = "Seznam ke stažení:",
                AutoSize = true,
                Location = new Point(24, 140)
            };
            Controls.Add(queueLabel);

            urlListBox.Location = new Point(24, 168);
            urlListBox.Size = new Size(ClientSize.Width - 48, 240);
            urlListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            urlListBox.HorizontalScrollbar = true;
            Controls.Add(urlListBox);

            removeButton.Text = "Odebrat vybraný";
            removeButton.Location = new Point(24, 420);
            removeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            removeButton.Size = new Size(150, 32);
            removeButton.Click += (_, _) =>
            {
                if (urlListBox.SelectedIndex >= 0)
                    urlListBox.Items.RemoveAt(urlListBox.SelectedIndex);
            };
            Controls.Add(removeButton);

            clearButton.Text = "Vymazat seznam";
            clearButton.Location = new Point(184, 420);
            clearButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            clearButton.Size = new Size(140, 32);
            clearButton.Click += (_, _) => urlListBox.Items.Clear();
            Controls.Add(clearButton);

            downloadButton.Text = "Stáhnout MP3 na USB";
            downloadButton.Location = new Point(ClientSize.Width - 190, 460);
            downloadButton.Size = new Size(166, 42);
            downloadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            downloadButton.Click += DownloadButton_Click;
            Controls.Add(downloadButton);

            progressBar.Location = new Point(24, ClientSize.Height - 44);
            progressBar.Size = new Size(ClientSize.Width - 48, 12);
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progressBar);

            statusLabel.Text = "Přetáhněte sem odkaz nebo ho vložte výše.";
            statusLabel.AutoEllipsis = true;
            statusLabel.Location = new Point(24, ClientSize.Height - 70);
            statusLabel.Size = new Size(ClientSize.Width - 48, 22);
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(statusLabel);

            foreach (Control control in Controls)
            {
                if (control.AllowDrop)
                    continue;

                control.AllowDrop = true;
                control.DragEnter += HandleDragEnter;
                control.DragDrop += HandleDrop;
            }

            Resize += (_, _) =>
            {
                urlBox.Width = ClientSize.Width - 150;
                addButton.Left = urlBox.Right + 10;
                urlListBox.Width = ClientSize.Width - 48;
                urlListBox.Height = ClientSize.Height - 340;
                removeButton.Top = ClientSize.Height - 160;
                clearButton.Top = ClientSize.Height - 160;
                downloadButton.Left = ClientSize.Width - 190;
                downloadButton.Top = ClientSize.Height - 120;
                statusLabel.Top = ClientSize.Height - 70;
                statusLabel.Width = ClientSize.Width - 48;
                progressBar.Top = ClientSize.Height - 44;
                progressBar.Width = ClientSize.Width - 48;
            };
        }

        private void UrlBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            AddUrl(urlBox.Text);
        }

        private void HandleDragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = TryGetDroppedText(e.Data, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void HandleDrop(object? sender, DragEventArgs e)
        {
            if (!TryGetDroppedText(e.Data, out var text))
                return;

            int added = 0;
            foreach (var candidate in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (AddUrl(candidate, showErrors: false))
                    added++;
            }

            statusLabel.Text = added > 0
                ? $"Přidáno odkazů: {added}."
                : "Přetažený obsah neobsahoval platný odkaz na YouTube.";
        }

        private static bool TryGetDroppedText(IDataObject? data, out string text)
        {
            text = string.Empty;
            if (data is null)
                return false;

            foreach (var format in new[] { DataFormats.UnicodeText, DataFormats.Text, "UniformResourceLocatorW", "UniformResourceLocator" })
            {
                if (!data.GetDataPresent(format))
                    continue;

                var value = data.GetData(format);
                if (value is string stringValue)
                {
                    text = stringValue;
                    return true;
                }

                if (value is MemoryStream stream)
                {
                    stream.Position = 0;
                    using var reader = new StreamReader(stream, format.EndsWith("W", StringComparison.Ordinal)
                        ? Encoding.Unicode
                        : Encoding.Default, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                    text = reader.ReadToEnd().TrimEnd('\0', '\r', '\n');
                    return true;
                }
            }

            return false;
        }

        private bool AddUrl(string value, bool showErrors = true)
        {
            var url = value.Trim();
            if (!IsYouTubeUrl(url))
            {
                if (showErrors)
                    MessageBox.Show("Zadejte platný odkaz na YouTube.", "Neplatný odkaz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!urlListBox.Items.Contains(url))
                urlListBox.Items.Add(url);

            urlBox.Clear();
            statusLabel.Text = "Odkaz byl přidán do seznamu.";
            return true;
        }

        private static bool IsYouTubeUrl(string value)
        {
            return Downloads.IsYouTubeUrl(value);
        }

        private async void DownloadButton_Click(object? sender, EventArgs e)
        {
            if (urlListBox.Items.Count == 0)
            {
                MessageBox.Show("Nejdříve přidejte do seznamu alespoň jeden odkaz.", "Prázdný seznam", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var destination = ChooseRemovableDrive();
            if (destination is null)
                return;

            downloadButton.Enabled = false;
            addButton.Enabled = false;
            removeButton.Enabled = false;
            clearButton.Enabled = false;

            int completed = 0;
            var failures = new List<string>();
            try
            {
                var queue = urlListBox.Items.Cast<string>().ToList();
                foreach (var url in queue)
                {
                    statusLabel.Text = $"Stahuji: {url}";
                    progressBar.Value = 0;
                    var progress = new Progress<double>(value =>
                        progressBar.Value = Math.Clamp((int)(value * 100), 0, 100));

                    try
                    {
                        await downloads.DownloadToFolderAsync(url, destination, progress);
                        urlListBox.Items.Remove(url);
                        completed++;
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{url}: {ex.Message}");
                    }
                }
            }
            finally
            {
                progressBar.Value = 0;
                downloadButton.Enabled = true;
                addButton.Enabled = true;
                removeButton.Enabled = true;
                clearButton.Enabled = true;
            }

            if (failures.Count == 0)
            {
                statusLabel.Text = $"Hotovo. Staženo skladeb: {completed}.";
                MessageBox.Show($"Staženo skladeb: {completed}.\nSoubory jsou uložené na {destination}.",
                    "Stahování dokončeno", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            statusLabel.Text = $"Staženo: {completed}. Neúspěšné odkazy zůstaly v seznamu: {failures.Count}.";
            MessageBox.Show($"Staženo skladeb: {completed}.\n\nNepodařilo se stáhnout:\n{string.Join("\n", failures)}",
                "Některá stahování selhala", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string? ChooseRemovableDrive()
        {
            var drives = DriveInfo.GetDrives()
                .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                .ToList();
            if (drives.Count == 0)
            {
                MessageBox.Show("Nenašel se žádný připravený vyměnitelný disk. Připojte USB disk a zkuste to znovu.",
                    "USB disk nebyl nalezen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using var dialog = new Form
            {
                Text = "Vyberte cílový USB disk",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(390, 140)
            };

            var label = new Label
            {
                Text = "Kam chcete uložit stažené skladby?",
                AutoSize = true,
                Location = new Point(16, 16)
            };
            var driveBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, 46),
                Width = 358,
                DataSource = drives.Select(drive => new DriveChoice(drive)).ToList()
            };
            var okButton = new Button
            {
                Text = "Vybrat",
                DialogResult = DialogResult.OK,
                Location = new Point(198, 92),
                Size = new Size(84, 30)
            };
            var cancelButton = new Button
            {
                Text = "Zrušit",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 92),
                Size = new Size(84, 30)
            };
            dialog.Controls.AddRange([label, driveBox, okButton, cancelButton]);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            return dialog.ShowDialog(this) == DialogResult.OK && driveBox.SelectedItem is DriveChoice choice
                ? choice.Drive.RootDirectory.FullName
                : null;
        }

        private sealed class DriveChoice(DriveInfo drive)
        {
            public DriveInfo Drive { get; } = drive;

            public override string ToString()
            {
                var label = string.IsNullOrWhiteSpace(Drive.VolumeLabel) ? "Vyměnitelný disk" : Drive.VolumeLabel;
                return $"{label} ({Drive.Name})";
            }
        }
    }
}
