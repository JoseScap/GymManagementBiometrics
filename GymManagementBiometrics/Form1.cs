using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using libzkfpcsharp;
using Sample;

namespace GymManagementBiometrics
{
    public partial class Form1 : Form
    {
        NotifyIcon notifyIcon;
        bool isExiting = false;

        IList<int> _devices = new List<int>();
        IntPtr _deviceHandle = IntPtr.Zero;
        IntPtr _formHandle = IntPtr.Zero;
        bool _isTimeToDie = false;

        byte[] _fingerprintBuffer;
        byte[] _tempCapture = new byte[2048];
        int _cbTempCapture = 2048;

        int _fingerprintWidth = 0;
        int _fingerprintHeight = 0;

        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();
        }

        //private void InitializeNotifyIcon()
        //{
        //    notifyIcon = new NotifyIcon();

        //    string basePath = AppDomain.CurrentDomain.BaseDirectory;
        //    string iconPath = Path.Combine(basePath, "icons", "gym_management.ico");

        //    notifyIcon.Icon = new Icon(iconPath);
        //    notifyIcon.Text = "Gym Management Biometrics";
        //    notifyIcon.Visible = true;

        //    ContextMenuStrip contextMenu = new ContextMenuStrip();
        //    contextMenu.Items.Add("Abrir", null, (s, e) => ShowWindow());
        //    contextMenu.Items.Add("Salir", null, (s, e) => ExitApplication());
        //    notifyIcon.ContextMenuStrip = contextMenu;

        //    notifyIcon.DoubleClick += (s, e) => ShowWindow();
        //}

        //private void ShowWindow()
        //{
        //    this.Show();
        //    this.WindowState = FormWindowState.Normal;
        //    this.ShowInTaskbar = true;
        //}

        //private void ExitApplication()
        //{
        //    isExiting = true;
        //    notifyIcon.Visible = false;
        //    notifyIcon.Dispose();
        //    Application.Exit();
        //}

        //private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    if (isExiting)
        //    {
        //        base.OnFormClosing(e);
        //    }
        //    else if (e.CloseReason == CloseReason.UserClosing)
        //    {
        //        e.Cancel = true;
        //        this.Hide();
        //        this.ShowInTaskbar = false;
        //    }
        //}

        private void Form1_Load(object sender, EventArgs e)
        {
            Log("GymManagementBiometrics");
            _formHandle = this.Handle;
        }

        private void Log(string message, string scope = "No Scope")
        {
            LoggingGridView.Rows.Add($"[{scope}] - {message}");
        }

        private void ButtonInit_Click(object sender, EventArgs e)
        {
            try
            {
                Log("Click", ButtonInit.Name);
                Log("Iniciando coneccion con los recursos del sistema", ButtonInit.Name);
                int result = zkfperrdef.ZKFP_ERR_OK;
                if ((result = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
                {
                    Log("La coneccion con los recursos del sistema se realizo exitosamente", ButtonInit.Name);
                    int count = zkfp2.GetDeviceCount();
                    Log("Cantidad de dispositivos detectados: " + count, ButtonInit.Name);
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            _devices.Add(i);
                        }
                    }
                    else
                    {
                        zkfp2.Terminate();
                        Log("No hay dispositivos conectados!", ButtonInit.Name);
                    }
                }
                else
                {
                    Log("No se pudo abrir coneccion con los recursos del sistema. ERROR: " + result, ButtonInit.Name);
                }

                if (_devices.Count > 0)
                {
                    ConnectDevices();
                }
            }
            catch (Exception ex)
            {
                Log("Ocurrio un error grabe al iniciar conexion con los recursos del sistema.", ButtonInit.Name);
                Log(ex.Message, ButtonInit.Name);
                Log(ex.StackTrace, ButtonInit.Name);
            }
        }

        private void ConnectDevices()
        {
            try
            {
                Log("Iniciando coneccion con los dispositivos conectados al sistema", ButtonInit.Name);
                for (int i = 0; i < _devices.Count; i++)
                {
                    Log("Iniciando coneccion con el dispositivo" + i, ButtonInit.Name);
                    IntPtr result = IntPtr.Zero;
                    if (IntPtr.Zero == (_deviceHandle = zkfp2.OpenDevice(i)))
                    {
                        Log("OpenDevice fail", ButtonInit.Name);
                        continue;
                    }

                    byte[] parameterValue = new byte[4];
                    int size = 4;
                    zkfp2.GetParameters(_deviceHandle, 1, parameterValue, ref size);
                    zkfp2.ByteArray2Int(parameterValue, ref _fingerprintWidth);

                    size = 4;
                    zkfp2.GetParameters(_deviceHandle, 2, parameterValue, ref size);
                    zkfp2.ByteArray2Int(parameterValue, ref _fingerprintHeight);

                    _fingerprintBuffer = new byte[_fingerprintWidth * _fingerprintHeight];

                    Thread captureThread = new Thread(new ThreadStart(DoCapture));
                    captureThread.IsBackground = true;
                    captureThread.Start();
                    break;
                }
            }
            catch (Exception ex)
            {
                Log("Ocurrio un error grabe al abrir coneccion con los dispositivos.", ButtonInit.Name);
                Log(ex.Message, ButtonInit.Name);
                Log(ex.StackTrace, ButtonInit.Name);
            }
        }

        private void DoCapture()
        {
            while (!_isTimeToDie)
            {
                _cbTempCapture = 2048;
                int result = zkfp2.AcquireFingerprint(_deviceHandle, _fingerprintBuffer, _tempCapture, ref _cbTempCapture);
                if (result == zkfp.ZKFP_ERR_OK)
                {
                    SendMessage(_formHandle, MESSAGE_CAPTURED_OK, IntPtr.Zero, IntPtr.Zero);
                }
                Thread.Sleep(200);
            }
        }

        protected override void DefWndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case MESSAGE_CAPTURED_OK:
                    {
                        Log("Huella leida", "CRONOS");
                        MemoryStream memoryStream = new MemoryStream();
                        BitmapFormat.GetBitmap(_fingerprintBuffer, _fingerprintWidth, _fingerprintHeight, ref memoryStream);
                        Bitmap bitmap = new Bitmap(memoryStream);
                        this.FingerprintBox.Image = bitmap;

                        string basePath = AppDomain.CurrentDomain.BaseDirectory;
                        string fingerprintsFolder = Path.Combine(basePath, "fingerprints");
                        if (!Directory.Exists(fingerprintsFolder))
                        {
                            Directory.CreateDirectory(fingerprintsFolder);
                        }

                        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                        string randomHash = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8); // 8 caracteres de un hash aleatorio
                        string fileName = $"{timestamp}_{randomHash}_huella.jpg";

                        string filePath = Path.Combine(fingerprintsFolder, fileName);

                        Log($"Imagen: {filePath}", "CRONOS");

                        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    break;

                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
    }
}
