using Microsoft.Playwright;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            };
        }

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
                    string crnsText = data.crns;
                    string asunto = data.asunto;
                    string medio = data.medio;
                    string tipo = data.tipo;
                    string resultado = data.resultado;
                    string descripcion = data.descripcion;

                    string[] listaCrns = crnsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    await ProcesarToques(listaCrns, asunto, medio, tipo, resultado, descripcion);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error interno en WebView: " + ex.Message);
            }
        }

        // ====================================================================
        // CEREBRO DEL BOT REESTRUCTURADO Y BLINDADO
        // ====================================================================
        // ====================================================================
        // CEREBRO DEL BOT: VERSIÓN AUTÓNOMA CON REPORTE FINAL
        // ====================================================================
        public async Task ProcesarToques(string[] listaCrns, string asunto, string medio, string tipo, string resultado, string descripcion)
        {
            var playwright = await Playwright.CreateAsync();
            string perfilNavegador = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PerfilCRM");

            var context = await playwright.Chromium.LaunchPersistentContextAsync(perfilNavegador, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = ViewportSize.NoViewport,
                Args = new[] { "--start-maximized" }
            });

            var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();

            await page.GotoAsync("https://scolaristalisis.crm.dynamics.com/main.aspx?appid=f77ed2cf-97b1-44a4-be42-5fad4d4caf84&pagetype=dashboard&id=0ab00b54-5899-eb11-b1ac-000d3a8dd852&type=system&_canOverride=true");

            await page.WaitForSelectorAsync("[data-id='searchLauncher']", new() { State = WaitForSelectorState.Visible, Timeout = 0 });

            List<string> errores = new List<string>();
            string fechaActual = DateTime.Now.ToString("dd/MM/yyyy");

            foreach (var crn in listaCrns)
            {
                try
                {
                    // ====================================================================
                    // 2. BUSCADOR GLOBAL
                    // ====================================================================
                    await page.ClickAsync("[data-id='searchLauncher']");
                    await page.FillAsync("[data-id='categorized-search-text-input']", crn);
                    await page.Keyboard.PressAsync("Enter");

                    // ====================================================================
                    // 3. VALIDACIÓN (ACEPTA MÚLTIPLES TIPOS DE LICENCIATURA)
                    // ====================================================================
                    // Usamos Regex para decirle: "Acepta Licenciatura Ejecutiva O Licenciatura normal"
                    var filaAlumno = page.Locator("button[role='link']").Filter(new LocatorFilterOptions
                    {
                        HasTextRegex = new System.Text.RegularExpressions.Regex("Licenciatura Ejecutiva|Licenciatura")
                    });

                    try
                    {
                        await filaAlumno.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
                    }
                    catch (TimeoutException)
                    {
                        // Si falla, lo guarda silenciosamente en la lista y pasa al siguiente
                        errores.Add($"CRN {crn}: No se encontró la tarjeta del alumno.");
                        continue;
                    }

                    await filaAlumno.First.ClickAsync();
                    await Task.Delay(4000); // Espera estable para asegurar la carga del perfil

                    // ====================================================================
                    // 4. ABRIR MENÚ DE ACTIVIDADES PENDIENTES
                    // ====================================================================
                    var btnTresPuntos = page.GetByRole(AriaRole.Menuitem, new() { Name = "Más comandos para Actividad" }).First;
                    await btnTresPuntos.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    await btnTresPuntos.ClickAsync();

                    await Task.Delay(1500);

                    var btnCrearActividad = page.GetByRole(AriaRole.Menuitem, new() { Name = "Crear Actividad", Exact = false }).First;
                    await btnCrearActividad.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    await btnCrearActividad.ClickAsync();

                    await Task.Delay(1000);

                    var btnInteraccion = page.GetByRole(AriaRole.Menuitem, new() { Name = "Interacción digital", Exact = false }).First;
                    await btnInteraccion.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    await btnInteraccion.ClickAsync();

                    await Task.Delay(3000);

                    // ====================================================================
                    // 5. LLENADO DEL FORMULARIO 
                    // ====================================================================
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

                    // ====================================================================
                    // 6. GUARDAR Y COMPLETAR
                    // ====================================================================
                    await page.ClickAsync("[data-id='scolaris_interacciondigital|NoRelationship|Form|Mscrm.Form.scolaris_interacciondigital.SaveAsComplete']");

                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    // Atrapa cualquier error inesperado de forma silenciosa
                    errores.Add($"CRN {crn}: Error en el proceso ({ex.Message})");
                }
            }

            // ====================================================================
            // 7. REPORTE FINAL (HISTORIAL DE LA SESIÓN ACTUAL)
            // ====================================================================
            if (errores.Count > 0)
            {
                string reporte = "El bot finalizó la ejecución.\n\nLos siguientes CRNs no se pudieron procesar:\n\n" + string.Join("\n", errores);
                MessageBox.Show(reporte, "Reporte de Errores", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("¡Todos los CRNs de la lista se procesaron y guardaron correctamente!", "Éxito Total", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void webView_Click(object sender, EventArgs e) { }
    }
}