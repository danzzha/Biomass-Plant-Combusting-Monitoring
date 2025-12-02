using MathNet.Numerics.IntegralTransforms;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ProgramSPS
{
    public partial class Form1 : Form
    {
        private ComboBox cmbSensor;
        private Button btnGenerate;
        private Chart chartTime, chartFreq, chartS, chartZ;
        private Label lblTitle, lblFormula;
        private Panel panelHeader, panelControl, panelParameters;
        private TrackBar[] trackParams = new TrackBar[3];
        private Label[] lblParams = new Label[3];
        private Label[] lblValues = new Label[3];

        // Tab Control untuk switching antara Sensor dan 3D Model
        private TabControl mainTabControl;
        private TabPage tabSensor, tab3DModel;

        // WebView2 untuk menampilkan 3D model
        private WebView2 webView3D;

        private const double samplingRate = 100;
        private const int numSamples = 200;
        private List<double> signalBuffer = new List<double>();
        private Random random = new Random();
        private int selectedSensorIndex = 0;

        // Parameter arrays untuk setiap sensor (disesuaikan dengan variabel rumus)
        private double[] param1 = { 0.21, 5.0, 50.0, 1000.0, 25.0 };      // P_O2, V_out, RH, Q, Temperature
        private double[] param2 = { 298.0, 10.0, 25.0, 1.225, 0.0 };      // T, S, T_ref, ρ, (not used)
        private double[] param3 = { 0.0, 0.0, 0.0, 100.0, 0.0 };          // (not used), (not used), (not used), A, (not used)

        private string[] sensorNames = {
            "Oxygen Sensor - O₂",
            "Pressure Sensor",
            "Humidity Sensor - RH",
            "Flow Sensor",
            "Thermocouple Type K"
        };

        private string[] formulas = {
            "E_O₂ = (R·T / 4F) · ln(0.21 / P_O₂)",
            "P = V_out / S",
            "RH = (P_v / P_sat(T)) × 100%",
            "Q = A · √(2ΔP / ρ)",
            "T = Temperature (°C)"
        };

        private string[,] paramLabels = {
            { "P_O₂ (atm):", "T (Temperature K):", "(Reserved):" },
            { "V_out (Voltage V):", "S (Sensitivity mV/Pa):", "(Reserved):" },
            { "RH (%):", "T (Temperature °C):", "T_ref (°C):" },
            { "Q (Flow m³/s):", "ρ (Density kg/m³):", "A (Area m²):" },
            { "Temperature (°C):", "(Reserved):", "(Reserved):" }
        };

        private double[,] paramRanges = {
            // Oxygen Sensor: P_O2, T, reserved
            { 0.10, 0.30,    273, 373,      0, 1 },
            // Pressure Sensor: V_out, S, reserved
            { 1.0, 10.0,     5.0, 20.0,     0, 1 },
            // Humidity Sensor: RH, T, T_ref
            { 20, 95,        10, 40,        15, 30 },
            // Flow Sensor: Q, ρ, A
            { 500, 2000,     1.0, 1.5,      50, 200 },
            // Thermocouple: Temperature, reserved, reserved
            { -50, 150,      0, 1,          0, 1 }
        };

        public Form1()
        {
            InitializeComponent();
        }

        private async void InitializeComponent()
        {
            this.Text = "Sensor Processing System - Physics-Based Mode with 3D Viewer";
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Header
            panelHeader = new Panel { BackColor = Color.FromArgb(25, 118, 210), Dock = DockStyle.Top, Height = 60 };
            lblTitle = new Label
            {
                Text = "BIOMASS PLANT COMBUSTING MONITORING - PHYSICS-BASED SIGNAL",
                Location = new Point(20, 15),
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };
            panelHeader.Controls.Add(lblTitle);

            // ===== TAB CONTROL =====
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // TAB 1: Sensor Processing
            tabSensor = new TabPage("📊 Sensor Processing & Signals");

            // TAB 2: 3D Model Viewer
            tab3DModel = new TabPage("🎨 3D Model Viewer");

            mainTabControl.TabPages.Add(tabSensor);
            mainTabControl.TabPages.Add(tab3DModel);

            // ===== SENSOR TAB CONTENT =====
            // Control Panel
            panelControl = new Panel { BackColor = Color.White, Dock = DockStyle.Top, Height = 100 };

            cmbSensor = new ComboBox
            {
                Location = new Point(20, 15),
                Size = new Size(380, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cmbSensor.Items.AddRange(sensorNames);
            cmbSensor.SelectedIndex = 0;
            cmbSensor.SelectedIndexChanged += (s, e) => { selectedSensorIndex = cmbSensor.SelectedIndex; UpdateUI(); };

            btnGenerate = new Button
            {
                Location = new Point(420, 15),
                Size = new Size(150, 30),
                Text = "🔄 GENERATE SIGNAL",
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += (s, e) => GenerateSignal();

            lblFormula = new Label
            {
                Location = new Point(20, 60),
                Size = new Size(1150, 25),
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 40, 80)
            };

            panelControl.Controls.AddRange(new Control[] { cmbSensor, btnGenerate, lblFormula });

            // Parameter Panel
            panelParameters = new Panel { BackColor = Color.FromArgb(255, 248, 225), Dock = DockStyle.Top, Height = 70 };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                int xPos = 20 + i * 440;

                lblParams[i] = new Label { Location = new Point(xPos, 15), Size = new Size(160, 20), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
                trackParams[i] = new TrackBar { Location = new Point(xPos + 165, 10), Size = new Size(160, 45), Minimum = 0, Maximum = 1000, Value = 500, TickStyle = TickStyle.None };
                lblValues[i] = new Label { Location = new Point(xPos + 330, 15), Size = new Size(90, 20), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.DarkBlue };

                trackParams[i].Scroll += (s, e) => { UpdateParameterValue(idx); GenerateSignal(); };

                panelParameters.Controls.AddRange(new Control[] { lblParams[i], trackParams[i], lblValues[i] });
            }

            // Charts
            TableLayoutPanel chartPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                ColumnCount = 2,
                RowCount = 2
            };
            chartPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            chartPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            chartTime = CreateChart("Time Domain - Sensor Output", "Sample (n)", "Output Value", Color.FromArgb(33, 150, 243));
            chartFreq = CreateChart("Discrete Fourier Transform (DFT)", "Frequency Bin (k)", "Magnitude", Color.FromArgb(156, 39, 176));
            chartS = CreateChart("S-Domain (Pole-Zero)", "Real (σ)", "Imaginary (jω)", Color.FromArgb(255, 152, 0));
            chartZ = CreateChart("Z-Domain (Pole-Zero)", "Real", "Imaginary", Color.FromArgb(0, 150, 136));

            chartPanel.Controls.Add(chartTime, 0, 0);
            chartPanel.Controls.Add(chartFreq, 1, 0);
            chartPanel.Controls.Add(chartS, 0, 1);
            chartPanel.Controls.Add(chartZ, 1, 1);

            tabSensor.Controls.Add(chartPanel);
            tabSensor.Controls.Add(panelParameters);
            tabSensor.Controls.Add(panelControl);

            // ===== 3D MODEL TAB CONTENT =====
            Initialize3DViewer();

            // Add components to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(panelHeader);

            UpdateUI();
            GenerateSignal();
        }

        private async void Initialize3DViewer()
        {
            // Panel untuk kontrol 3D viewer
            Panel panel3DControl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White
            };

            Label lbl3DTitle = new Label
            {
                Text = "🎨 Biomass Plant 3D Model Viewer",
                Location = new Point(20, 15),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 118, 210),
                AutoSize = true
            };

            Label lbl3DInfo = new Label
            {
                Text = "Rotate: Left Mouse | Zoom: Mouse Wheel | Pan: Right Mouse",
                Location = new Point(20, 45),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                AutoSize = true
            };

            // Tombol untuk reload model
            Button btnReloadModel = new Button
            {
                Location = new Point(20, 65),
                Size = new Size(120, 25),
                Text = "🔄 Reload Model",
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnReloadModel.FlatAppearance.BorderSize = 0;
            btnReloadModel.Click += async (s, e) =>
            {
                try
                {
                    if (webView3D.CoreWebView2 != null)
                    {
                        await Load3DModelAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reloading: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            panel3DControl.Controls.AddRange(new Control[] { lbl3DTitle, lbl3DInfo, btnReloadModel });

            // WebView2 untuk render 3D
            webView3D = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            tab3DModel.Controls.Add(webView3D);
            tab3DModel.Controls.Add(panel3DControl);

            try
            {
                // Initialize WebView2 dengan environment khusus
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), "WebView2Cache"));
                await webView3D.EnsureCoreWebView2Async(env);

                // Tunggu sebentar untuk memastikan WebView2 ready
                await Task.Delay(500);

                // Load 3D scene
                await Load3DModelAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing 3D viewer: {ex.Message}\n\nStack: {ex.StackTrace}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Load3DModelAsync()
        {
            try
            {
                string html = GetThreeJsHTML();

                // Gunakan data URI untuk menghindari masalah dengan file path
                string dataUri = "data:text/html;charset=utf-8," + Uri.EscapeDataString(html);

                // Navigate dengan timeout
                var cts = new System.Threading.CancellationTokenSource(5000);
                webView3D.CoreWebView2.Navigate(dataUri);

                await Task.Delay(100); // Small delay for stability
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading 3D model: {ex.Message}");
                // Fallback: coba dengan metode sederhana
                try
                {
                    webView3D.CoreWebView2.NavigateToString(GetSimpleHTML());
                }
                catch
                {
                    MessageBox.Show("Unable to load 3D viewer. Please check WebView2 installation.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private string GetSimpleHTML()
        {
            // HTML sederhana untuk testing tanpa model 3D
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { margin: 0; overflow: hidden; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        #canvas-container { width: 100vw; height: 100vh; }
    </style>
</head>
<body>
    <div id='canvas-container'></div>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>
    <script>
        let scene, camera, renderer, cube;

        function init() {
            scene = new THREE.Scene();
            scene.background = new THREE.Color(0x1a1a2e);

            camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
            camera.position.z = 5;

            renderer = new THREE.WebGLRenderer({ antialias: true });
            renderer.setSize(window.innerWidth, window.innerHeight);
            document.getElementById('canvas-container').appendChild(renderer.domElement);

            const geometry = new THREE.BoxGeometry(2, 2, 2);
            const material = new THREE.MeshStandardMaterial({ 
                color: 0x667eea,
                metalness: 0.5,
                roughness: 0.3
            });
            cube = new THREE.Mesh(geometry, material);
            scene.add(cube);

            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
            scene.add(ambientLight);

            const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
            directionalLight.position.set(5, 5, 5);
            scene.add(directionalLight);

            const gridHelper = new THREE.GridHelper(20, 20, 0x667eea, 0x444444);
            scene.add(gridHelper);

            window.addEventListener('resize', onWindowResize, false);
        }

        function onWindowResize() {
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        }

        function animate() {
            requestAnimationFrame(animate);
            cube.rotation.x += 0.01;
            cube.rotation.y += 0.01;
            renderer.render(scene, camera);
        }

        init();
        animate();
    </script>
</body>
</html>";
        }

        private string GetThreeJsHTML()
        {
            // Coba beberapa lokasi untuk file GLB
            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "model.glb"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.glb"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "model.glb"),
                Path.Combine(Directory.GetCurrentDirectory(), "model.glb")
            };

            string glbPath = null;
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    glbPath = path;
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"GLB Path: {glbPath}");
            System.Diagnostics.Debug.WriteLine($"File Exists: {(glbPath != null)}");

            // Jika tidak ada model, gunakan cube default
            if (glbPath == null)
            {
                MessageBox.Show("File model.glb tidak ditemukan!\n\nLokasi yang dicek:\n" +
                    string.Join("\n", possiblePaths),
                    "Model Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return GetSimpleHTML();
            }

            // Baca file dan konversi ke base64 (hanya jika file kecil < 10MB)
            FileInfo fileInfo = new FileInfo(glbPath);
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                MessageBox.Show("File GLB terlalu besar (>10MB). Gunakan file yang lebih kecil.",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return GetSimpleHTML();
            }

            byte[] fileBytes = File.ReadAllBytes(glbPath);
            string base64Model = Convert.ToBase64String(fileBytes);

            // Return HTML dengan model 3D yang di-embed sebagai base64
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ margin: 0; overflow: hidden; background: #1a1a2e; }}
        #canvas-container {{ width: 100vw; height: 100vh; }}
        #loading {{ 
            position: absolute; 
            top: 50%; 
            left: 50%; 
            transform: translate(-50%, -50%);
            color: white;
            font-family: 'Segoe UI', sans-serif;
            font-size: 18px;
        }}
    </style>
</head>
<body>
    <div id='loading'>Loading 3D Model...</div>
    <div id='canvas-container'></div>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>
    <script>
        let scene, camera, renderer, model;
        let mouseX = 0, mouseY = 0;
        let targetX = 0, targetY = 0;

        function init() {{
            scene = new THREE.Scene();
            scene.background = new THREE.Color(0x1a1a2e);

            camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
            camera.position.set(0, 2, 5);
            camera.lookAt(0, 0, 0);

            renderer = new THREE.WebGLRenderer({{ antialias: true }});
            renderer.setSize(window.innerWidth, window.innerHeight);
            renderer.shadowMap.enabled = true;
            document.getElementById('canvas-container').appendChild(renderer.domElement);

            // Lighting
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
            scene.add(ambientLight);

            const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
            directionalLight.position.set(5, 10, 5);
            directionalLight.castShadow = true;
            scene.add(directionalLight);

            const pointLight = new THREE.PointLight(0x667eea, 1, 100);
            pointLight.position.set(0, 5, 0);
            scene.add(pointLight);

            // Grid
            const gridHelper = new THREE.GridHelper(20, 20, 0x667eea, 0x444444);
            scene.add(gridHelper);

            // Load GLB Model or Fallback
            loadFallbackModel();

            // Mouse interaction
            document.addEventListener('mousemove', onMouseMove, false);
            window.addEventListener('resize', onWindowResize, false);

            document.getElementById('loading').style.display = 'none';
        }}

        function loadFallbackModel() {{
            // Create a biomass plant representation
            const group = new THREE.Group();

            // Base cylinder (main body)
            const bodyGeometry = new THREE.CylinderGeometry(1, 1.2, 3, 32);
            const bodyMaterial = new THREE.MeshStandardMaterial({{ 
                color: 0x667eea, 
                metalness: 0.6, 
                roughness: 0.4 
            }});
            const body = new THREE.Mesh(bodyGeometry, bodyMaterial);
            body.castShadow = true;
            group.add(body);

            // Top dome
            const domeGeometry = new THREE.SphereGeometry(1, 32, 16, 0, Math.PI * 2, 0, Math.PI / 2);
            const dome = new THREE.Mesh(domeGeometry, bodyMaterial);
            dome.position.y = 1.5;
            group.add(dome);

            // Chimney
            const chimneyGeometry = new THREE.CylinderGeometry(0.3, 0.3, 2, 16);
            const chimneyMaterial = new THREE.MeshStandardMaterial({{ 
                color: 0x888888, 
                metalness: 0.8, 
                roughness: 0.2 
            }});
            const chimney = new THREE.Mesh(chimneyGeometry, chimneyMaterial);
            chimney.position.set(0.8, 2, 0);
            group.add(chimney);

            // Pipes
            for(let i = 0; i < 3; i++) {{
                const pipeGeometry = new THREE.CylinderGeometry(0.15, 0.15, 1.5, 16);
                const pipe = new THREE.Mesh(pipeGeometry, chimneyMaterial);
                const angle = (i * 120) * Math.PI / 180;
                pipe.position.set(Math.cos(angle) * 1.3, -0.5, Math.sin(angle) * 1.3);
                pipe.rotation.z = Math.PI / 2;
                pipe.rotation.y = angle;
                group.add(pipe);
            }}

            // Base platform
            const platformGeometry = new THREE.CylinderGeometry(1.5, 1.5, 0.2, 32);
            const platformMaterial = new THREE.MeshStandardMaterial({{ 
                color: 0x444444, 
                metalness: 0.7, 
                roughness: 0.3 
            }});
            const platform = new THREE.Mesh(platformGeometry, platformMaterial);
            platform.position.y = -1.6;
            group.add(platform);

            model = group;
            scene.add(model);
        }}

        function onMouseMove(event) {{
            mouseX = (event.clientX / window.innerWidth) * 2 - 1;
            mouseY = -(event.clientY / window.innerHeight) * 2 + 1;
        }}

        function onWindowResize() {{
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        }}

        function animate() {{
            requestAnimationFrame(animate);

            // Smooth mouse following
            targetX = mouseX * 0.3;
            targetY = mouseY * 0.3;

            if (model) {{
                model.rotation.y += (targetX - model.rotation.y) * 0.05;
                model.rotation.x += (targetY - model.rotation.x) * 0.05;
                model.rotation.y += 0.002; // Slow auto-rotation
            }}

            renderer.render(scene, camera);
        }}

        init();
        animate();
    </script>
</body>
</html>";
        }

        private Chart CreateChart(string title, string xTitle, string yTitle, Color color)
        {
            Chart chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(5) };

            ChartArea area = new ChartArea { BackColor = Color.WhiteSmoke };
            area.AxisX.Title = xTitle;
            area.AxisY.Title = yTitle;
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;
            chart.ChartAreas.Add(area);

            Series series = new Series("Signal")
            {
                ChartType = SeriesChartType.Line,
                Color = color,
                BorderWidth = 2
            };
            chart.Series.Add(series);

            chart.Titles.Add(new Title(title, Docking.Top, new Font("Segoe UI", 10F, FontStyle.Bold), Color.FromArgb(33, 33, 33)));

            return chart;
        }

        private void UpdateUI()
        {
            lblFormula.Text = "📘 Formula: " + formulas[selectedSensorIndex];
            for (int i = 0; i < 3; i++)
            {
                lblParams[i].Text = paramLabels[selectedSensorIndex, i];

                // Hide trackbar and value if parameter is not used
                bool isUsed = !paramLabels[selectedSensorIndex, i].Contains("Reserved");
                trackParams[i].Visible = isUsed;
                lblValues[i].Visible = isUsed;

                if (isUsed)
                {
                    UpdateParameterValue(i);
                }
            }
        }

        private void UpdateParameterValue(int idx)
        {
            int sIdx = selectedSensorIndex;
            double min = paramRanges[sIdx, idx * 2];
            double max = paramRanges[sIdx, idx * 2 + 1];
            double value = min + (max - min) * trackParams[idx].Value / 1000.0;

            if (idx == 0) param1[sIdx] = value;
            else if (idx == 1) param2[sIdx] = value;
            else param3[sIdx] = value;

            string format = (max - min < 1) ? "F3" : (max - min < 10) ? "F2" : "F1";
            lblValues[idx].Text = value.ToString(format);
        }

        private void GenerateSignal()
        {
            signalBuffer.Clear();

            for (int n = 0; n < numSamples; n++)
            {
                double t = n / samplingRate;
                double signal = 0;

                // Hitung sinyal berdasarkan rumus fisika masing-masing sensor
                switch (selectedSensorIndex)
                {
                    case 0: // Oxygen Sensor
                        {
                            double PO2 = param1[0];
                            double T = param2[0];
                            const double R = 8.314;
                            const double F = 96485;
                            const double PO2_ref = 0.21;
                            double PO2Dynamic = PO2 * (1 + 0.1 * Math.Sin(2 * Math.PI * 0.2 * t));
                            if (PO2Dynamic <= 0) PO2Dynamic = 0.001;
                            signal = (R * T / (4 * F)) * Math.Log(PO2_ref / PO2Dynamic);
                            signal += 0.001 * Math.Sin(2 * Math.PI * 0.05 * t);
                            signal += 0.0005 * (random.NextDouble() - 0.5);
                        }
                        break;

                    case 1: // Pressure Sensor
                        {
                            double Vout = param1[1];
                            double sensitivity = param2[1];
                            double VoutDynamic = Vout * (1 + 0.3 * Math.Sin(2 * Math.PI * 1.5 * t));
                            signal = VoutDynamic / sensitivity;
                            signal += 0.1 * signal * Math.Sin(2 * Math.PI * 3.0 * t);
                            signal += 0.02 * signal * (random.NextDouble() - 0.5);
                        }
                        break;

                    case 2: // Humidity Sensor
                        {
                            double RH = param1[2];
                            double T = param2[2];
                            double T_ref = param3[2];
                            double T_dynamic = T + 2 * Math.Sin(2 * Math.PI * 0.3 * t);
                            double P_sat = 6.1078 * Math.Exp((17.27 * T_dynamic) / (T_dynamic + 237.3));
                            double P_sat_ref = 6.1078 * Math.Exp((17.27 * T_ref) / (T_ref + 237.3));
                            double RH_dynamic = RH * (1 + 0.15 * Math.Sin(2 * Math.PI * 0.5 * t));
                            signal = RH_dynamic * (P_sat_ref / P_sat);
                            signal += 1.0 * (random.NextDouble() - 0.5);
                        }
                        break;

                    case 3: // Flow Sensor
                        {
                            double Q = param1[3];
                            double rho = param2[3];
                            double A = param3[3];
                            double deltaP = (Q * Q * rho) / (2 * A * A);
                            double deltaPDynamic = deltaP * (1 + 0.25 * Math.Sin(2 * Math.PI * 0.8 * t));
                            signal = A * Math.Sqrt(2 * deltaPDynamic / rho);
                            signal += 0.05 * signal * (random.NextDouble() - 0.5);
                        }
                        break;

                    case 4: // Thermocouple
                        {
                            double temperature = param1[4];
                            double tempDynamic = temperature + 5 * Math.Sin(2 * Math.PI * 0.15 * t);
                            tempDynamic += 2 * Math.Sin(2 * Math.PI * 0.05 * t);
                            signal = tempDynamic;
                            signal += 0.3 * (random.NextDouble() - 0.5);
                        }
                        break;
                }

                signalBuffer.Add(signal);
            }

            UpdateAllCharts();
        }

        private void UpdateAllCharts()
        {
            // Time Domain
            chartTime.Series[0].Points.Clear();
            for (int i = 0; i < signalBuffer.Count; i++)
                chartTime.Series[0].Points.AddXY(i, signalBuffer[i]);

            // Frequency Domain
            UpdateFrequencyDomain();

            // S & Z Domains
            UpdateSDomain();
            UpdateZDomain();
        }

        private void UpdateFrequencyDomain()
        {
            int fftSize = 256;
            Complex[] data = new Complex[fftSize];

            for (int i = 0; i < fftSize; i++)
            {
                if (i < signalBuffer.Count)
                {
                    double w = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (signalBuffer.Count - 1));
                    data[i] = new Complex(signalBuffer[i] * w, 0);
                }
                else data[i] = Complex.Zero;
            }

            Fourier.Forward(data, FourierOptions.Matlab);

            chartFreq.Series[0].Points.Clear();
            chartFreq.Series[0].ChartType = SeriesChartType.Column;
            chartFreq.Series[0].BorderWidth = 2;

            for (int i = 0; i < fftSize / 2; i++)
            {
                double magnitude = data[i].Magnitude / fftSize;
                chartFreq.Series[0].Points.AddXY(i, magnitude);
            }

            if (chartFreq.Series.Count < 2)
            {
                Series markers = new Series("DFT Points")
                {
                    ChartType = SeriesChartType.Point,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    MarkerColor = Color.FromArgb(156, 39, 176),
                    Color = Color.FromArgb(156, 39, 176),
                    IsVisibleInLegend = false
                };
                chartFreq.Series.Add(markers);
            }

            chartFreq.Series[1].Points.Clear();
            for (int i = 0; i < fftSize / 2; i++)
            {
                double magnitude = data[i].Magnitude / fftSize;
                chartFreq.Series[1].Points.AddXY(i, magnitude);
            }
        }

        private void UpdateSDomain()
        {
            EnsurePoleZeroSeries(chartS);
            chartS.Series[1].Points.Clear();
            chartS.Series[2].Points.Clear();

            double scale = 1.0;
            switch (selectedSensorIndex)
            {
                case 0: scale = 0.2; break;
                case 1: scale = 1.5; break;
                case 2: scale = 0.5; break;
                case 3: scale = 0.8; break;
                case 4: scale = 0.15; break;
            }

            chartS.Series[1].Points.AddXY(-4 * scale, 0);
            chartS.Series[1].Points.AddXY(-5 * scale, 0);
            chartS.Series[1].Points.AddXY(-8 * scale, 0);

            double maxSigma = 10 * scale;
            chartS.ChartAreas[0].AxisX.Minimum = -maxSigma;
            chartS.ChartAreas[0].AxisX.Maximum = 1;
            chartS.ChartAreas[0].AxisY.Minimum = -2;
            chartS.ChartAreas[0].AxisY.Maximum = 2;
        }

        private void UpdateZDomain()
        {
            EnsurePoleZeroSeries(chartZ);
            chartZ.Series[1].Points.Clear();
            chartZ.Series[2].Points.Clear();
            chartZ.ChartAreas[0].AxisX.Minimum = -1.5;
            chartZ.ChartAreas[0].AxisX.Maximum = 1.5;
            chartZ.ChartAreas[0].AxisY.Minimum = -1.5;
            chartZ.ChartAreas[0].AxisY.Maximum = 1.5;

            if (chartZ.Series.Count < 4)
            {
                Series circle = new Series("Unit") { ChartType = SeriesChartType.Line, Color = Color.Gray, BorderWidth = 1, IsVisibleInLegend = false };
                chartZ.Series.Add(circle);
                for (int i = 0; i <= 360; i += 5)
                {
                    double angle = i * Math.PI / 180.0;
                    circle.Points.AddXY(Math.Cos(angle), Math.Sin(angle));
                }
            }

            double T = 1.0 / samplingRate;
            double omega = 0;

            switch (selectedSensorIndex)
            {
                case 0: omega = 2 * Math.PI * 0.2; break;
                case 1: omega = 2 * Math.PI * 1.5; break;
                case 2: omega = 2 * Math.PI * 0.5; break;
                case 3: omega = 2 * Math.PI * 0.8; break;
                case 4: omega = 2 * Math.PI * 0.15; break;
            }

            double damping = -0.1;
            Complex s1 = new Complex(damping, omega);
            Complex s2 = new Complex(damping, -omega);
            Complex z1 = Complex.Exp(s1 * T);
            Complex z2 = Complex.Exp(s2 * T);

            chartZ.Series[1].Points.AddXY(z1.Real, z1.Imaginary);
            chartZ.Series[1].Points.AddXY(z2.Real, z2.Imaginary);

            if (selectedSensorIndex == 1)
            {
                Complex s3 = new Complex(damping, 3 * omega);
                Complex z3 = Complex.Exp(s3 * T);
                chartZ.Series[1].Points.AddXY(z3.Real, z3.Imaginary);
                chartZ.Series[1].Points.AddXY(z3.Real, -z3.Imaginary);
            }

            chartZ.Series[2].Points.AddXY(0, 0);
        }

        private void EnsurePoleZeroSeries(Chart chart)
        {
            if (chart.Series.Count < 2)
                chart.Series.Add(new Series("Poles") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Cross, MarkerSize = 10, MarkerColor = Color.Red });
            if (chart.Series.Count < 3)
                chart.Series.Add(new Series("Zeros") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Circle, MarkerSize = 10, MarkerColor = Color.Blue });
        }
    }
}