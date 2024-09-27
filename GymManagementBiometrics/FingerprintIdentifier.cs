using GymManagementBiometrics.Messages;
using libzkfpcsharp;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Sample;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GymManagementBiometrics
{
    public partial class FingerprintIdentifier : Form
    {
        #region Cronos Vars
        // Lista de dispositivos
        IList<int> _devices = new List<int>();

        // Identificadores del dispositivo
        IntPtr _deviceHandle = IntPtr.Zero;
        IntPtr _cacheHandle = IntPtr.Zero;
        IntPtr _formHandle = IntPtr.Zero;

        // Medidas del dispositivo & buffer
        int _fingerprintWidth = 0;
        int _fingerprintHeight = 0;
        byte[] _fingerprintBuffer;

        // Capturas
        byte[][] _tempTemplates = new byte[3][];
        byte[] _tempMergedTemplate = new byte[2048];
        byte[] _tempCapture = new byte[2048];
        int _cbTempCapture = 2048;
        int _cbMergedTemplate = 0;

        // Banderas del sistema
        bool _isTimeToDie = false;
        bool _isRegister = false;

        // Contadores del sistema
        int _registerCount = 0;
        #endregion Cronos Vars

        #region Socket vars
        SocketIOClient.SocketIO _client;
        #endregion
        
        #region System Vars
        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;
        #endregion System Vars

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public FingerprintIdentifier()
        {
            InitializeComponent();
        }

        private void FingerprintIdentifier_Load(object sender, EventArgs e)
        {
            SetupApp();
        }

        public void SetupApp()
        {
            _formHandle = this.Handle;
            this.Status.Text = _isRegister == true ? "Registrando" : "Identificando";
            CreateSocketServerAsync();
            DetectDevices();
        }

        private async Task CreateSocketServerAsync()
        {
            _client = new SocketIOClient.SocketIO("http://localhost:3000");

            _client.On("Bio:Ping", (data) =>
            {
                var dataAsString = data.ToString();
                List<PingMessage> deserializedData = JsonConvert.DeserializeObject<List<PingMessage>>(dataAsString);

                _client.EmitAsync("Bio:Pong", deserializedData[0]);
            });

            await _client.ConnectAsync();
        }

        private void DetectDevices()
        {
            try
            {
                Console.WriteLine("Click");
                Console.WriteLine("Iniciando coneccion con los recursos del sistema");
                int result = zkfperrdef.ZKFP_ERR_OK;
                if ((result = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
                {
                    Console.WriteLine("La coneccion con los recursos del sistema se realizo exitosamente");
                    int count = zkfp2.GetDeviceCount();
                    Console.WriteLine("Cantidad de dispositivos detectados: " + count);
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
                        Console.WriteLine("No hay dispositivos conectados!" );
                    }
                }
                else
                {
                    Console.WriteLine("No se pudo abrir coneccion con los recursos del sistema. ERROR: " + result);
                }

                if (_devices.Count > 0)
                {
                    ConnectDevices();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrio un error grabe al iniciar conexion con los recursos del sistema.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void CleanTempTemplates()
        {
            for (int i = 0; i < 3; i++)
            {
                _tempTemplates[i] = new byte[2048];
            }
        }

        private void ConnectDevices()
        {
            try
            {
                Console.WriteLine("Iniciando coneccion con los dispositivos conectados al sistema");
                CleanTempTemplates();

                for (int i = 0; i < _devices.Count; i++)
                {
                    Console.WriteLine("Iniciando coneccion con el dispositivo" + i);
                    IntPtr result = IntPtr.Zero;
                    if (IntPtr.Zero == (_deviceHandle = zkfp2.OpenDevice(i)))
                    {
                        Console.WriteLine("OpenDevice fail");
                        continue;
                    }
                    if (IntPtr.Zero == (_cacheHandle = zkfp2.DBInit()))
                    {
                        Console.WriteLine("Init DB fail");
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

                //if (_cacheHandle != IntPtr.Zero) PopulateDevice();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocurrio un error grabe al abrir coneccion con los dispositivos.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
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
            switch (m.Msg)
            {
                case MESSAGE_CAPTURED_OK:
                    {
                        // !!! Estas 5 lineas solo sirven para convertir la lectura en imagen
                        Console.WriteLine("Huella leida");
                        MemoryStream memoryStream = new MemoryStream();
                        BitmapFormat.GetBitmap(_fingerprintBuffer, _fingerprintWidth, _fingerprintHeight, ref memoryStream);
                        Bitmap bitmap = new Bitmap(memoryStream);
                        MemoryStream jpgStream = new MemoryStream();
                        
                        this.FingerprintBox.Image = bitmap;

                        if (_isRegister)
                        {
                            int result = zkfp.ZKFP_ERR_OK;
                            // Agregamos la huella a un array de capturas temporales
                            Array.Copy(_tempCapture, _tempTemplates[_registerCount], _cbTempCapture);
                            _registerCount++;
                            Console.WriteLine("Huella agregada. N" + _registerCount);
                            if (_registerCount >= 3)
                            {
                                _registerCount = 0;
                                if (zkfp.ZKFP_ERR_OK == (result = zkfp2.DBMerge(_cacheHandle, _tempTemplates[0], _tempTemplates[1], _tempTemplates[2], _tempMergedTemplate, ref _cbMergedTemplate)))
                                {
                                    Console.WriteLine("Template generada.");
                                    string base64Template = zkfp2.BlobToBase64(_tempMergedTemplate, _cbMergedTemplate);
                                    Console.WriteLine("Template Base 64:");
                                    Console.WriteLine(base64Template);

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
                                                Console.WriteLine("Huella almacenada con éxito en la base de datos.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error al almacenar la huella: {ex.Message}");
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
                                Console.WriteLine("Identify succ, fid= " + fid + ",score=" + score + "!");
                                return;
                            }
                            else
                            {
                                Console.WriteLine("Identify fail, ret= " + result);
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
    }
}
