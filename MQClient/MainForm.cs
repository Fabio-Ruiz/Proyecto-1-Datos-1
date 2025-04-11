// Espacios de nombres y dependencias
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQClientGUIApp
{
    public partial class MainForm : Form
    {
        private MQClient client; // Instancia del cliente que gestiona la conexión
        private Guid appId = Guid.NewGuid(); // Identificador único del cliente
        private bool conectado = false; // Indica si el cliente está actualmente conectado al broker

        // Constructor que crea la ventana principal y dibuja todos los controles en ella al iniciarla
        public MainForm()
        {
            InitializeComponent();
        }

        // Valida que una dirección IP sea IPv4 (por ejemplo, 127.0.0.1)
        bool EsIPv4Valida(string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var address))
                return false;

            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.Trim().Split('.').Length == 4;
        }

        // Aquí se construye manualmente la interfaz gráfica
        private void InitializeComponent()
        {
            Text = "Message Queue Client";
            Width = 450;
            Height = 630;

            // === Controles de IP y Puerto ===
            var lblIp = new Label() { Text = "MQ Broker IP:", Top = 10, Left = 180, Width = 80 };
            var txtIp = new TextBox() { Top = 32, Left = 160, Width = 120 };

            var lblPort = new Label() { Text = "MQ Broker Port:", Top = 70, Left = 175 };
            var txtPort = new TextBox() { Top = 90, Left = 195, Width = 50 };

            // Botón para conexión
            var btnConnect = new Button() { Text = "Conectar", Top = 120, Left = 180, Width = 80 };

            // Muestra el AppID
            var lblAppId = new Label() { Text = $"AppID: {appId}", Top = 150, Left = 20, Width = 400, ForeColor = Color.Red };

            // Muestra los textos: Topic, Message y Messages Received
            var lblTopic = new Label() { Text = "Topic:", Top = 180, Left = 20, Width = 40 };
            var lblMessage = new Label() { Text = "Message:", Top = 250, Left = 20, Width = 60 };
            var lblReceived = new Label() { Text = "Messages Received:", Top = 380, Left = 150, Width = 120 };

            // Cajas de texto para escribir Topic y Messages
            var txtTopic = new TextBox() { Top = 180, Left = 100, Width = 300 };
            var txtMessage = new TextBox() { Top = 250, Left = 100, Width = 300, Height = 80, Multiline = true };

            var btnSubscribe = new Button() { Text = "Subscribe", Top = 210, Left = 150 };
            var btnUnsubscribe = new Button() { Text = "Unsubscribe", Top = 210, Left = 270, Width = 80 };
            var btnPublish = new Button() { Text = "Publish", Top = 280, Left = 15 };
            var btnReceive = new Button() { Text = "Receive", Top = 560, Left = 170 };

            // Caja de texto que muestra los mensajes recibidos
            var lstMessages = new ListBox() { Top = 410, Left = 20, Width = 380, Height = 140, DrawMode = DrawMode.OwnerDrawVariable };

            // Esto permite que los mensajes recibidos se vean con altura automática y diseño personalizado
            lstMessages.DrawItem += (s, e) =>
            {
                if (e.Index >= 0)
                {
                    string itemText = lstMessages.Items[e.Index].ToString();
                    e.DrawBackground();
                    e.Graphics.DrawString(itemText, lstMessages.Font, Brushes.Black, e.Bounds);
                    e.DrawFocusRectangle();
                }
            };

            lstMessages.MeasureItem += (s, e) =>
            {
                string itemText = lstMessages.Items[e.Index].ToString();
                SizeF textSize = e.Graphics.MeasureString(itemText, lstMessages.Font, lstMessages.Width);
                e.ItemHeight = (int)Math.Ceiling(textSize.Height);
            };


            // Deshabilitar botones hasta conectar
            btnSubscribe.Enabled = btnUnsubscribe.Enabled = btnPublish.Enabled = btnReceive.Enabled = false;

            // Si no está conectado, intenta conectarse y habilita botones
            // Si ya está conectado, se desconecta y desactiva los botones
            // Incluye validación de IP y puerto
            btnConnect.Click += (s, e) =>
            {
                if (!conectado)
                {
                    string ip = txtIp.Text.Trim();
                    string portText = txtPort.Text.Trim();

                    if (!EsIPv4Valida(ip))
                    {
                        MessageBox.Show("La IP ingresada no es válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
                    {
                        MessageBox.Show("El puerto debe ser un número entre 1 y 65535.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        // Crear cliente
                        client = new MQClient(ip, port, appId);

                        // Intentar una suscripción de prueba para validar conexión
                        bool success = client.Connect();

                        if (success)
                        {
                            conectado = true;
                            btnConnect.Text = "Desconectar";
                            btnSubscribe.Enabled = btnUnsubscribe.Enabled = btnPublish.Enabled = btnReceive.Enabled = true;

                            MessageBox.Show("Conectado correctamente al broker.");
                        }
                        else
                        {
                            MessageBox.Show("No se pudo establecer conexión con el broker.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al conectar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    try
                    {
                        if (client != null)
                        {
                            bool ok = client.Disconnect();
                            if (ok)
                            {
                                MessageBox.Show("Desconectado del broker.");
                            }
                            else
                            {
                                MessageBox.Show("No se pudo notificar la desconexión al broker.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al desconectar: " + ex.Message);
                    }

                    conectado = false;
                    btnConnect.Text = "Conectar";
                    btnSubscribe.Enabled = btnUnsubscribe.Enabled = btnPublish.Enabled = btnReceive.Enabled = false;
                }

            };

            // Botón para suscribirse a un tema
            btnSubscribe.Click += (s, e) =>
            {
                try
                {
                    if (client.Subscribe(new Topic(txtTopic.Text)))
                        MessageBox.Show("Subscribed successfully.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Botón para desuscribirse a un tema
            btnUnsubscribe.Click += (s, e) =>
            {
                try
                {
                    if (client.Unsubscribe(new Topic(txtTopic.Text)))
                        MessageBox.Show("Unsubscribed successfully.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Botón para enviar un mensaje a un tema
            btnPublish.Click += (s, e) =>
            {
                try
                {
                    if (client.Publish(new Message(txtMessage.Text), new Topic(txtTopic.Text)))
                        MessageBox.Show("Message published.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Botón para solicitar un mensaje al topic actua
            // Si recibe uno, lo muestra con la hora en el ListBox
            btnReceive.Click += (s, e) =>
            {
                try
                {
                    Topic tpc = new Topic(txtTopic.Text);
                    Message msg = client.Receive(tpc);
                    lstMessages.Items.Add($"[{DateTime.Now:T}] [{tpc}] {msg}");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            // Agregan los controles visuales al formulario principal para que se vean y funcionen
            Controls.Add(lblIp);
            Controls.Add(txtIp);
            Controls.Add(lblPort);
            Controls.Add(txtPort);
            Controls.Add(btnConnect);
            Controls.Add(lblAppId);
            Controls.Add(lblTopic);
            Controls.Add(lblMessage);
            Controls.Add(lblReceived);
            Controls.Add(txtTopic);
            Controls.Add(txtMessage);
            Controls.Add(btnSubscribe);
            Controls.Add(btnUnsubscribe);
            Controls.Add(btnPublish);
            Controls.Add(btnReceive);
            Controls.Add(lstMessages);
        }

        // Inicia la ventana principal del cliente
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}