using Microsoft.Playwright; // ¡NUEVO! Necesario para el bot
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic; // ¡NUEVO! Necesario para las listas
using System.IO;
using System.Threading.Tasks; // ¡NUEVO! Necesario para procesos en segundo plano
using System.Windows.Forms;

namespace TOQUES
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
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

                // Aquí le decimos que escuche al HTML
                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            };
        }

        // AQUÍ ES DONDE CONECTAMOS EL HTML CON C Y PLAYWRIGHT
        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonRecibido = e.TryGetWebMessageAsString();

            try
            {
                dynamic data = JsonConvert.DeserializeObject(jsonRecibido);
                string command = data.command;

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
                    this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                }
                else if (command == "StartToques")
                {

                    // 1. Extraemos los datos que mandó el Zorro
                    string crnsText = data.crns;
                    string asunto = data.asunto;
                    string medio = data.medio;
                    string tipo = data.tipo;
                    string resultado = data.resultado;
                    string descripcion = data.descripcion;

                    // 2. Convertimos el texto de CRNs en una lista real separando por saltos de línea
                    string[] listaCrns = crnsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // 3. ¡DESATAMOS AL BOT DE PLAYWRIGHT!
                    await ProcesarToques(listaCrns, asunto, medio, tipo, resultado, descripcion);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error interno: " + ex.Message);
            }
        }

        // ====================================================================
        // AQUÍ PEGAS EL CEREBRO DEL BOT QUE CONSTRUIMOS (ProcesarToques)
        // ====================================================================
        public async Task ProcesarToques(string[] listaCrns, string asunto, string medio, string tipo, string resultado, string descripcion)
        {
            // Inicializar Playwright
            var playwright = await Playwright.CreateAsync();

            // Headless = false permite ver el navegador
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();

            // 1. IR AL CRM (¡CAMBIA ESTO POR LA URL REAL DE LA UNIVERSIDAD!)
            await page.GotoAsync("https://scolaristalisis.crm.dynamics.com/main.aspx?appid=f77ed2cf-97b1-44a4-be42-5fad4d4caf84&pagetype=dashboard&id=0ab00b54-5899-eb11-b1ac-000d3a8dd852&type=system&_canOverride=true");

            // 2. PAUSA INTELIGENTE PARA INICIO DE SESIÓN
            // El bot esperará infinitamente (Timeout = 0) hasta que tú inicies sesión 
            // y la lupa de búsqueda aparezca en pantalla.
            await page.WaitForSelectorAsync("[data-id='searchLauncher']", new() { State = WaitForSelectorState.Visible, Timeout = 0 });

            List<string> errores = new List<string>();
            string fechaActual = DateTime.Now.ToString("dd/MM/yyyy");

            // --- A PARTIR DE AQUÍ EL CÓDIGO SIGUE EXACTAMENTE IGUAL ---
            foreach (var crn in listaCrns)
            {
                try
                {
                    // 2. BUSCADOR GLOBAL
                    await page.ClickAsync("[data-id='searchLauncher']");
                    // ... (resto del código del foreach)
                    await page.FillAsync("[data-id='categorized-search-text-input']", crn);
                    await page.Keyboard.PressAsync("Enter");

                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    // 3. VALIDACIÓN DE LICENCIATURA EJECUTIVA
                    var filaAlumno = page.Locator(".detailsContainer-347").Filter(new() { HasText = "Licenciatura Ejecutiva" });

                    if (await filaAlumno.CountAsync() == 0)
                    {
                        errores.Add($"CRN {crn}: Omitido (No es Licenciatura Ejecutiva o no se encontró).");
                        continue;
                    }

                    await filaAlumno.First.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    // 4. MENÚ DE ACTIVIDADES PENDIENTES
                    await page.ClickAsync("[data-id='OverflowButton']");
                    await page.GetByText("Crear Actividad", new() { Exact = true }).ClickAsync();
                    await page.GetByText("Interacción digital", new() { Exact = true }).ClickAsync();

                    // 5. LLENADO DEL FORMULARIO 
                    await page.FillAsync("[data-id='subject.fieldControl-text-box-text']", asunto);

                    await page.ClickAsync("[data-id='scolaris_mediodigital.fieldControl-option-set-select']");
                    await page.GetByRole(AriaRole.Option, new() { Name = medio, Exact = true }).ClickAsync();

                    var inputsFecha = page.GetByPlaceholder("dd/MM/yyyy");
                    await inputsFecha.Nth(0).FillAsync(fechaActual);
                    await inputsFecha.Nth(1).FillAsync(fechaActual);
                    await page.Keyboard.PressAsync("Escape");

                    await page.ClickAsync("[data-id='scolaris_tipointeraccion.fieldControl-option-set-select']");
                    await page.GetByRole(AriaRole.Option, new() { Name = tipo, Exact = true }).ClickAsync();

                    await page.ClickAsync("[data-id='scolaris_resultado.fieldControl-option-set-select']");
                    await page.GetByRole(AriaRole.Option, new() { Name = resultado, Exact = true }).ClickAsync();

                    await page.FillAsync("[data-id='description.fieldControl-text-box-text']", descripcion);

                    // 6. GUARDAR Y COMPLETAR
                    await page.ClickAsync("[data-id='scolaris_interacciondigital|NoRelationship|Form|Mscrm.Form.scolaris_interacciondigital.SaveAsComplete']");

                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    errores.Add($"CRN {crn}: Error inesperado - {ex.Message}");
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void webView_Click(object sender, EventArgs e) { }
    }
}