using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace TOQUES
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            // TAMAÑO INICIAL MÁS GRANDE
            this.Size = new System.Drawing.Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeWebView();
        }

        private void InitializeWebView()
        {
            this.Load += async (s, e) =>
            {
                await webView.EnsureCoreWebView2Async(null);
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "retro_bot_ui.html");

                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate(htmlPath);
                }
                else
                {
                    MessageBox.Show("¡Error crítico! No se encontró el diseño retro en:\n" + htmlPath, "Fallo del Sistema");
                }

                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            };
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonRecibido = e.TryGetWebMessageAsString();

            try
            {
                dynamic data = JsonConvert.DeserializeObject(jsonRecibido);
                string command = data.command;

                // NUEVOS CONTROLES DE VENTANA
                if (command == "CloseApp")
                {
                    this.Close();
                }
                else if (command == "MinimizeApp")
                {
                    this.WindowState = FormWindowState.Minimized;
                }
                else if (command == "MaximizeApp")
                {
                    // Si ya está maximizado, lo regresa a la normalidad; si no, lo maximiza.
                    this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                }
                else if (command == "StartToques")
                {
                    string crns = data.crns;
                    string msg = data.msg;
                    // (Lógica de Playwright pendiente)
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error interno: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void webView_Click(object sender, EventArgs e) { }
    }
}