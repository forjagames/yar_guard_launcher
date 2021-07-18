using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;
using System.Xml;

namespace UniversalGameLauncher
{
    public partial class Application : Form
    {
        private DownloadProgressTracker _downloadProgressTracker;

        public Version LocalVersion
        {
            get
            {
                if (!File.Exists("local_version.txt"))
                {
                    return new Version(0, 0, 0, 1);
                }

                return new Version(File.ReadAllText("local_version.txt"));
            }
        }
        public Version OnlineVersion { get; private set; }

        private List<PatchNoteBlock> patchNoteBlocks = new List<PatchNoteBlock>();

        private bool _isReady;
        public bool IsReady
        {
            get
            {
                return _isReady;
            }
            set
            {
                _isReady = value;
                TogglePlayButton(value);
                InitializeFooter();
            }
        }

        public bool UpToDate
        {
            get
            {
                if (OnlineVersion == null)
                {
                    return true;
                }

                return LocalVersion >= OnlineVersion;
            }
        }

        public Application()
        {
            InitializeComponent();
            int style = NativeWinAPI.GetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE);
            style |= NativeWinAPI.WS_EX_COMPOSITED;
            NativeWinAPI.SetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE, style);
        }

        private void OnLoadApplication(object sender, EventArgs e)
        {
            InitializeConstantsSettings();
            InitializeFiles();
            InitializeImages();
            FetchPatchNotes();
            InitializeVersionControl();

            IsReady = UpToDate;

            _downloadProgressTracker = new DownloadProgressTracker(50, TimeSpan.FromMilliseconds(500));

            if (!UpToDate && ((LocalVersion.Major == 0 && LocalVersion.Minor == 0 && LocalVersion.Build == 0) || Constants.AUTOMATICALLY_BEGIN_UPDATING))
            {
                DownloadAndExtractLatestVersionAsync();
            }
        }

        private void InitializeConstantsSettings()
        {
            Name = Constants.GAME_TITLE;
            Text = Constants.LAUNCHER_NAME;
            SetUpButtonEvents();

            currentVersionLabel.Visible = Constants.SHOW_VERSION_TEXT;
        }

        private void InitializeFiles()
        {
            if (!Directory.Exists(Constants.DESTINATION_PATH))
            {
                Directory.CreateDirectory(Constants.DESTINATION_PATH);
            }
        }

        private void InitializeImages()
        {
            try
            {
                LoadApplicationIcon();

                navbarPanel.BackColor = Color.FromArgb(Constants.PANEL_ALPHA, 0, 0, 0); // Make panel background semi transparent
                logoPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                closePictureBox.SizeMode = PictureBoxSizeMode.CenterImage;              // Center the X icon
                minimizePictureBox.SizeMode = PictureBoxSizeMode.CenterImage;           // Center the - icon

                if (Constants.PANEL_ALPHA > 128)
                {
                    closePictureBox.BackColor = Color.FromArgb(255, closePictureBox.BackColor.R, closePictureBox.BackColor.G, closePictureBox.BackColor.B);
                    minimizePictureBox.BackColor = Color.FromArgb(255, minimizePictureBox.BackColor.R, minimizePictureBox.BackColor.G, minimizePictureBox.BackColor.B);
                }
                else
                {
                    closePictureBox.BackColor = Color.FromArgb(0, closePictureBox.BackColor.R, closePictureBox.BackColor.G, closePictureBox.BackColor.B);
                    minimizePictureBox.BackColor = Color.FromArgb(0, minimizePictureBox.BackColor.R, minimizePictureBox.BackColor.G, minimizePictureBox.BackColor.B);
                }

                this.LoadLogoImage();
                this.LoadBackgroundImage();
            }
            catch (Exception e)
            {
                MessageBox.Show("The launcher was unable to retrieve some game images from the server! " + e.Message, "Error");
            }
        }

        private void LoadLogoImage()
        {
            if (Constants.CACHE_IMAGES)
            {
                string filename = "logo_" + Constants.LOGO_URL.Substring(Constants.LOGO_URL.LastIndexOf('/') + 1);

                if (!File.Exists(filename))
                {
                    DownloadFileSync(Constants.LOGO_URL, filename);
                }

                logoPictureBox.LoadAsync(filename);
                return;
            }

            logoPictureBox.LoadAsync(Constants.LOGO_URL);
        }

        private Image SetImageOpacity(Image image, float opacity)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default,
                                                  ColorAdjustType.Bitmap);
                g.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height),
                                   0, 0, image.Width, image.Height,
                                   GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        private void LoadBackgroundImage()
        {
            if (Constants.CACHE_IMAGES)
            {
                string filename = "bg_" + Constants.BACKGROUND_URL.Substring(Constants.BACKGROUND_URL.LastIndexOf('/') + 1);

                if (!File.Exists(filename))
                {
                    DownloadFileSync(Constants.BACKGROUND_URL, filename);
                }

                BackgroundImage = SetImageOpacity(Image.FromFile(filename), 0.8f);
                //BackgroundImage = Image.FromFile(filename);
                return;
            }

            using (var webClient = new WebClient())
            {
                using (Stream stream = webClient.OpenRead(Constants.BACKGROUND_URL))
                {
                    BackgroundImage = Image.FromStream(stream);
                }
            }
        }

        private void DownloadFileSync(string url, string name)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(url, name);
            }
        }

        private void LoadApplicationIcon()
        {
            if (Constants.CACHE_IMAGES)
            {
                if (!File.Exists("app_icon.ico"))
                {
                    DownloadFileSync(Constants.APPLICATION_ICON_URL, "app_icon.ico");
                }

                using (var img = Image.FromFile("app_icon.ico"))
                {
                    using (var bm = new Bitmap(img))
                    {
                        Icon = Icon.FromHandle(bm.GetHicon());
                    }
                }

                return;
            }

            WebRequest request = (HttpWebRequest)WebRequest.Create(Constants.APPLICATION_ICON_URL);

            MemoryStream memStream;

            using (Stream response = request.GetResponse().GetResponseStream())
            {
                memStream = new MemoryStream();
                byte[] buffer = new byte[1024];
                int byteCount;

                do
                {
                    byteCount = response.Read(buffer, 0, buffer.Length);
                    memStream.Write(buffer, 0, byteCount);
                } while (byteCount > 0);
            }

            using (var bm = new Bitmap(Image.FromStream(memStream)))
            {
                if (bm != null)
                {
                    Icon = Icon.FromHandle(bm.GetHicon());
                }
            }
        }

        private void InitializeVersionControl()
        {
            currentVersionLabel.Text = LocalVersion.ToString();
            OnlineVersion = GetOnlineVersion();

            if (OnlineVersion != null && OnlineVersion != LocalVersion)
            {
                currentVersionLabel.Text += " (New version found: " + OnlineVersion.ToString() + ")";
            }
        }

        private void InitializeFooter()
        {
            if (IsReady)
            {
                updateProgressBar.Visible = false;
                clientReadyLabel.Visible = true;
            }
            else
            {
                updateProgressBar.Visible = true;
                clientReadyLabel.Visible = false;
            }
        }

        private Version GetOnlineVersion()
        {
            try
            {
                string onlineVersion = new WebClient().DownloadString(Constants.VERSION_URL);
                Console.WriteLine(LocalVersion >= new Version(onlineVersion));
                Version.TryParse(onlineVersion, out Version result);
                return result;
            }
            catch
            {
                MessageBox.Show("The launcher was unable to read the current client version from the server!", "Fatal error");
                return null;
            }
        }

        private void OnClickPlay(object sender, EventArgs e)
        {
            if (IsReady)
            {
                LaunchGame();
            }
            else
            {
                DownloadAndExtractLatestVersionAsync();
            }
        }

        private void DownloadAndExtractLatestVersionAsync()
        {
            this.playButton.Enabled = false;

            using (var webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(FinishDownloadAndExtractLatestVersion);
                webClient.DownloadFileAsync(new Uri(Constants.CLIENT_DOWNLOAD_URL), Constants.ZIP_PATH);
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _downloadProgressTracker.SetProgress(e.BytesReceived, e.TotalBytesToReceive);
            updateProgressBar.Value = e.ProgressPercentage;
            updateLabelText.Text = string.Format("Downloading: {0} of {1} @ {2}", StringUtility.FormatBytes(e.BytesReceived),
                StringUtility.FormatBytes(e.TotalBytesToReceive), _downloadProgressTracker.GetBytesPerSecondString());
        }

        private void FinishDownloadAndExtractLatestVersion(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                _downloadProgressTracker.Reset();
                updateLabelText.Text = "Download finished - extracting...";

                Extract extract = new Extract(this);
                extract.Run(SetLauncherReady);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.playButton.Enabled = true;
            }
        }

        private void SetLauncherReady(RunWorkerCompletedEventArgs args)
        {
            try
            {
                if (args.Error != null)
                {
                    throw new Exception(args.Error.Message);
                }

                if (args.Cancelled)
                {
                    throw new Exception("The download was cancelled.");
                }

                updateLabelText.Text = "";

                if (!File.Exists(Constants.GAME_EXECUTABLE_PATH))
                {
                    throw new Exception("Couldn't make a connection to the game server. Please try again later or inform the developer if the issue persists.");
                }

                SaveOnlineVersion();

                IsReady = true;

                if (Constants.AUTOMATICALLY_LAUNCH_GAME_AFTER_UPDATING)
                {
                    LaunchGame();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Update failed");
            }
            finally
            {
                try
                {
                    File.Delete(Constants.ZIP_PATH);
                }
                catch
                {
                    MessageBox.Show("Couldn't delete the downloaded zip file after extraction.");
                }

                this.playButton.Enabled = true;
            }
        }

        private void SaveOnlineVersion()
        {
            currentVersionLabel.Text = OnlineVersion.ToString();

            //Properties.Settings.Default.VersionText = OnlineVersion.ToString();
            //Properties.Settings.Default.Save();

            File.WriteAllText("local_version.txt", OnlineVersion.ToString());
        }

        private void FetchPatchNotes()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Constants.PATCH_NOTES_URL);

                foreach (XmlNode node in doc.DocumentElement)
                {
                    PatchNoteBlock block = new PatchNoteBlock();
                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                block.Title = node.ChildNodes[i].InnerText;
                                break;
                            case 1:
                                block.Text = node.ChildNodes[i].InnerText;
                                break;
                            case 2:
                                block.Link = node.ChildNodes[i].InnerText;
                                break;
                        }
                    }
                    patchNoteBlocks.Add(block);
                }
            }
            catch
            {
                patchContainerPanel.Visible = false;
                if (Constants.SHOW_ERROR_BOX_IF_PATCH_NOTES_DOWNLOAD_FAILS)
                    MessageBox.Show("The launcher was unable to retrieve patch notes from the server!");
            }

            var controles = new[] { patchPanel1, patchPanel2, patchPanel3 };
            foreach (var control in controles)
            {
                control.Visible = false;
            }

            Button[] patchButtons = { patchButton1, patchButton2, patchButton3 };
            Label[] patchTitleObjects = { patchTitle1, patchTitle2, patchTitle3 };
            Label[] patchTextObjects = { patchText1, patchText2, patchText3 };

            for (int i = 0; i < patchNoteBlocks.Count; i++)
            {
                controles[i].Visible = true;
                patchTitleObjects[i].Text = patchNoteBlocks[i].Title;
                patchTextObjects[i].Text = patchNoteBlocks[i].Text;
                patchButtons[i].Visible = !string.IsNullOrEmpty(patchNoteBlocks[i].Link);
            }
        }

        private void LaunchGame()
        {
            try
            {
                Process.Start(Constants.GAME_EXECUTABLE_PATH);
                Environment.Exit(0);
            }
            catch
            {
                IsReady = false;
                DownloadAndExtractLatestVersionAsync();
                MessageBox.Show("Couldn't locate the game executable! Attempting to redownload - please wait.", "Fatal Error");
            }
        }

        private void TogglePlayButton(bool toggle)
        {
            switch (toggle)
            {
                case true:
                    playButton.BackColor = Color.Green;
                    playButton.Text = "Play";
                    break;
                case false:
                    playButton.BackColor = Color.DeepSkyBlue;
                    playButton.Text = "Update";
                    break;
            }
        }

        // Move the form with LMB
        private void Application_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeWinAPI.ReleaseCapture();
                NativeWinAPI.SendMessage(Handle, NativeWinAPI.WM_NCLBUTTONDOWN, NativeWinAPI.HT_CAPTION, 0);
            }
        }

        private void SetUpButtonEvents()
        {
            Button[] buttons = { navbarButton1, navbarButton2, navbarButton3, navbarButton4, navbarButton5 };

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Click += new EventHandler(OnClickButton);
                buttons[i].Text = Constants.NAVBAR_BUTTON_TEXT_ARRAY[i];
            }
        }

        public void OnClickButton(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            switch (button.Name)
            {
                case nameof(navbarButton1):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_1_URL);
                    break;
                case nameof(navbarButton2):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_2_URL);
                    break;
                case nameof(navbarButton3):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_3_URL);
                    break;
                case nameof(navbarButton4):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_4_URL);
                    break;
                case nameof(navbarButton5):
                    System.Diagnostics.Process.Start(Constants.NAVBAR_BUTTON_5_URL);
                    break;

                case nameof(patchButton1):
                    Process.Start(patchNoteBlocks[0].Link);
                    break;
                case nameof(patchButton2):
                    Process.Start(patchNoteBlocks[1].Link);
                    break;
                case nameof(patchButton3):
                    Process.Start(patchNoteBlocks[2].Link);
                    break;
            }
        }

        private void OnMouseEnterIcon(object sender, EventArgs e)
        {
            var pictureBox = (PictureBox)sender;
            if (Constants.PANEL_ALPHA > 128)
            {
                pictureBox.BackColor = Color.FromArgb(180, pictureBox.BackColor.R, pictureBox.BackColor.G, pictureBox.BackColor.B);
            }
            else
            {
                pictureBox.BackColor = Color.FromArgb(50, pictureBox.BackColor.R, pictureBox.BackColor.G, pictureBox.BackColor.B);
            }
        }

        private void OnMouseLeaveIcon(object sender, EventArgs e)
        {
            var pictureBox = (PictureBox)sender;
            if (Constants.PANEL_ALPHA > 128)
            {
                pictureBox.BackColor = Color.FromArgb(255, pictureBox.BackColor.R, pictureBox.BackColor.G, pictureBox.BackColor.B);
            }
            else
            {
                pictureBox.BackColor = Color.FromArgb(0, pictureBox.BackColor.R, pictureBox.BackColor.G, pictureBox.BackColor.B);
            }
        }

        private void minimizePictureBox_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void closePictureBox_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
