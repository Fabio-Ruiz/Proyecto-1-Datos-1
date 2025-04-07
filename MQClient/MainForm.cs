using System;
using System.Drawing;
using System.Windows.Forms;

namespace MQClientGUIAppNET8
{
    public partial class MainForm : Form
    {
        private MQClient client;
        private Guid appId = Guid.NewGuid();

        public MainForm()
        {
            InitializeComponent();
        }

        private bool conectado = false;

        bool EsIPv4Valida(string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var address))
                return false;

            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.Trim().Split('.').Length == 4;
        }


        private void InitializeComponent()
        {
            Text = "Message Queue Client";
            Width = 450;
            Height = 630;

            // === Nuevos controles de IP y Puerto ===
            var lblIp = new Label() { Text = "MQ Broker IP:", Top = 10, Left = 180, Width = 80 };
            var txtIp = new TextBox() { Top = 32, Left = 160, Width = 120 };

            var lblPort = new Label() { Text = "MQ Broker Port:", Top = 70, Left = 175 };
            var txtPort = new TextBox() { Top = 90, Left = 195, Width = 50 };

            var btnConnect = new Button() { Text = "Conectar", Top = 120, Left = 180, Width = 80 };

            var lblAppId = new Label() { Text = $"AppID: {appId}", Top = 150, Left = 20, Width = 400, ForeColor = Color.Red };


            var lblTopic = new Label() { Text = "Topic:", Top = 180, Left = 20, Width = 40 };
            var lblMessage = new Label() { Text = "Message:", Top = 250, Left = 20, Width = 60 };
            var lblReceived = new Label() { Text = "Messages Received:", Top = 380, Left = 150, Width = 120 };

            var txtTopic = new TextBox() { Top = 180, Left = 100, Width = 300 };
            var txtMessage = new TextBox() { Top = 250, Left = 100, Width = 300, Height = 80, Multiline = true };

            var btnSubscribe = new Button() { Text = "Subscribe", Top = 210, Left = 150 };
            var btnUnsubscribe = new Button() { Text = "Unsubscribe", Top = 210, Left = 270, Width = 80 };
            var btnPublish = new Button() { Text = "Publish", Top = 280, Left = 15 };
            var btnReceive = new Button() { Text = "Receive", Top = 560, Left = 170 };

            var lstMessages = new ListBox() { Top = 410, Left = 20, Width = 380, Height = 140, DrawMode = DrawMode.OwnerDrawVariable };


            // Deshabilitar botones hasta conectar
            btnSubscribe.Enabled = btnUnsubscribe.Enabled = btnPublish.Enabled = btnReceive.Enabled = false;

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

            btnSubscribe.Click += (s, e) =>
            {
                try
                {
                    if (client.Subscribe(new Topic(txtTopic.Text)))
                        MessageBox.Show("Subscribed successfully.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            btnUnsubscribe.Click += (s, e) =>
            {
                try
                {
                    if (client.Unsubscribe(new Topic(txtTopic.Text)))
                        MessageBox.Show("Unsubscribed successfully.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            btnPublish.Click += (s, e) =>
            {
                try
                {
                    if (client.Publish(new Message(txtMessage.Text), new Topic(txtTopic.Text)))
                        MessageBox.Show("Message published.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

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

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}