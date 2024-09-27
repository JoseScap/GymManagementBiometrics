using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using libzkfpcsharp;
using Sample;
using System.Threading.Tasks;

namespace GymManagementBiometrics
{
    public class Fingerprint
    {
        public int Id { get; set; }
        public string FingerTemplate { get; set; }
    }

    public partial class Form1 : Form
    {
        //NotifyIcon notifyIcon;
        //bool isExiting = false;

        IList<int> _devices = new List<int>();
        IntPtr _deviceHandle = IntPtr.Zero;
        IntPtr _cacheHandle = IntPtr.Zero;
        IntPtr _formHandle = IntPtr.Zero;
        bool _isTimeToDie = false;
        bool _isRegister = false;
        int _registerCount = 0;

        byte[] _fingerprintBuffer;
        
        byte[][] _tempTemplates = new byte[3][];
        byte[] _tempMergedTemplate = new byte[2048];
        byte[] _tempCapture = new byte[2048];
        int _cbTempCapture = 2048;
        int _cbMergedTemplate = 0;

        int _fingerprintWidth = 0;
        int _fingerprintHeight = 0;

        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;

        SocketIOClient.SocketIO _client;


        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();
            CreateSocketServerAsync();
        }

        private async Task CreateSocketServerAsync()
        {
            _client = new SocketIOClient.SocketIO("http://localhost:3000");

            await _client.ConnectAsync();
        }

        private void UpdateRegisterStatus(bool registerStatus)
        {
            _isRegister = registerStatus;
            // Actualizar la UI u otras operaciones según sea necesario
            // Asegúrate de hacer esto en el hilo de la UI si estás actualizando controles de WinForms
            this.Invoke((MethodInvoker)delegate
            {
                // Aquí puedes actualizar la interfaz de usuario si es necesario
                // Por ejemplo, mostrar un mensaje en un Label o similar
            });
        }

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
                for (int i = 0; i < 3; i++)
                {
                    _tempTemplates[i] = new byte[2048];
                }

                for (int i = 0; i < _devices.Count; i++)
                {
                    Log("Iniciando coneccion con el dispositivo" + i, ButtonInit.Name);
                    IntPtr result = IntPtr.Zero;
                    if (IntPtr.Zero == (_deviceHandle = zkfp2.OpenDevice(i)))
                    {
                        Log("OpenDevice fail", ButtonInit.Name);
                        continue;
                    }
                    if (IntPtr.Zero == (_cacheHandle = zkfp2.DBInit()))
                    {
                        Log("Init DB fail", ButtonInit.Name);
                        zkfp2.CloseDevice(_deviceHandle);
                        _deviceHandle = IntPtr.Zero;
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

                if (_cacheHandle != IntPtr.Zero) PopulateDevice();
            }
            catch (Exception ex)
            {
                Log("Ocurrio un error grabe al abrir coneccion con los dispositivos.", ButtonInit.Name);
                Log(ex.Message, ButtonInit.Name);
                Log(ex.StackTrace, ButtonInit.Name);
            }
        }

        private void PopulateDevice() {
            // Cadena de conexión a la base de datos MySQL
            string connectionString = "Server=localhost;Database=test;User Id=gym_user;Password=19582016Silvia;";

            // Lista para almacenar los objetos Fingerprint
            List<Fingerprint> fingerprints = new List<Fingerprint>();

            try
            {
                // Conexión a la base de datos
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Consulta para obtener todos los id y huellas digitales
                    string query = "SELECT id, fingerTemplate FROM fingerprints";

                    // Comando para ejecutar la consulta
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        // Ejecutar la consulta y obtener los resultados
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Iterar sobre los resultados
                            while (reader.Read())
                            {
                                // Crear un objeto Fingerprint para cada registro
                                Fingerprint fingerprint = new Fingerprint
                                {
                                    Id = reader.GetInt32("id"),
                                    FingerTemplate = reader.GetString("fingerTemplate")
                                };
                                // Agregar el objeto a la lista
                                fingerprints.Add(fingerprint);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Log($"Error al leer las huellas digitales de la base de datos: {ex.Message}", "CRONOS");
            }

            // Segundo paso: Iterar sobre la lista 'fingerprints' para convertir y almacenar en _cache
            double totalSizeInKB = 0.0;
            foreach (var fingerprint in fingerprints)
            {
                // Log del fingerprint que se agrega (solo el id)
                Log($"Agregando fingerprint con ID: {fingerprint.Id}", "CRONOS");

                // Convertir la huella digital de Base64 a blob
                byte[] blob = zkfp.Base64String2Blob(fingerprint.FingerTemplate);
                double sizeInKB = blob.Length / 1024.0;
                totalSizeInKB += sizeInKB;

                // Añadir la huella digital a la caché
                int result = zkfp2.DBAdd(_cacheHandle, fingerprint.Id, blob);

                // Verificar si la operación fue exitosa
                if (result == zkfp.ZKFP_ERR_OK)
                {
                    Log($"Fingerprint con ID: {fingerprint.Id} agregado a la caché con éxito.", "CRONOS");
                    Log($"El tamaño del archivo es: {sizeInKB:F2} KB. Total acumulado {totalSizeInKB:F2}.", "CRONOS");
                }
                else
                {
                    Log($"Error al agregar fingerprint con ID: {fingerprint.Id} a la caché. Código de error: {result}", "CRONOS");
                }
            }
            Log($"Carga finalizada. Memoria ocupada {totalSizeInKB:F2} KB", "CRONOS");
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
                        // !!! Estas 5 lineas solo sirven para convertir la lectura en imagen
                        Log("Huella leida", "CRONOS");
                        MemoryStream memoryStream = new MemoryStream();
                        BitmapFormat.GetBitmap(_fingerprintBuffer, _fingerprintWidth, _fingerprintHeight, ref memoryStream);
                        Bitmap bitmap = new Bitmap(memoryStream);
                        this.FingerprintBox.Image = bitmap;
                        
                        if (_isRegister)
                        {
                            int result = zkfp.ZKFP_ERR_OK;
                            // Agregamos la huella a un array de capturas temporales
                            Array.Copy(_tempCapture, _tempTemplates[_registerCount], _cbTempCapture);
                            _registerCount++;
                            Log("Huella agregada. N" + _registerCount, "CRONOS");
                            if (_registerCount >= 3)
                            {
                                _registerCount = 0;
                                if (zkfp.ZKFP_ERR_OK == (result = zkfp2.DBMerge(_cacheHandle, _tempTemplates[0], _tempTemplates[1], _tempTemplates[2], _tempMergedTemplate, ref _cbMergedTemplate)))
                                {
                                    Log("Template generada.", "CRONOS");
                                    string base64Template = zkfp2.BlobToBase64(_tempMergedTemplate, _cbMergedTemplate);
                                    Log("Template Base 64:", "CRONOS");
                                    Log(base64Template, "CRONOS");

                                    // Aquí agregar lógica para almacenar la huella en la base de datos
                                    string connectionString = "Server=localhost;Database=test;User Id=gym_user;Password=19582016Silvia;";
                                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    {
                                        try
                                        {
                                            connection.Open();
                                            string query = "INSERT INTO fingerprints (fingerTemplate) VALUES (@fingerTemplate)";
                                            using (MySqlCommand cmd = new MySqlCommand(query, connection))
                                            {
                                                // Generar un UUID para el id
                                                string uuid = Guid.NewGuid().ToString();
                                                cmd.Parameters.AddWithValue("@fingerTemplate", base64Template);

                                                cmd.ExecuteNonQuery();
                                                Log("Huella almacenada con éxito en la base de datos.", "CRONOS");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log($"Error al almacenar la huella: {ex.Message}", "CRONOS");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            int result = zkfp.ZKFP_ERR_OK;
                            int fid = 0, score = 0;
                            result = zkfp2.DBIdentify(_cacheHandle, _tempCapture, ref fid, ref score);
                            if (zkfp.ZKFP_ERR_OK == result)
                            {
                                Log("Identify succ, fid= " + fid + ",score=" + score + "!", "CRONOS");
                                return;
                            }
                            else
                            {
                                Log("Identify fail, ret= " + result, "CRONOS");
                                return;
                            }
                        }
                    }
                    break;

                default:
                    base.DefWndProc(ref m);
                    break;
            }
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
    }
}
