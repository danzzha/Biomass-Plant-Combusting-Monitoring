[README.md](https://github.com/user-attachments/files/23884519/README.md)
# Biomass Plant Combustion Monitoring System

[![.NET](https://img.shields.io/badge/.NET-Framework-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Three.js](https://img.shields.io/badge/Three.js-r128-000000?logo=three.js)](https://threejs.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A Windows Forms application for real-time sensor signal processing and 3D visualization of biomass power plant systems. Features physics-based sensor modeling, advanced signal analysis, and interactive 3D rendering.

## Features

### Sensor Processing
- Five industrial sensor types with physics-based models
- Real-time signal generation and analysis
- Time domain, frequency domain (DFT), S-domain, and Z-domain visualization
- Dynamic parameter adjustment with instant feedback

### Supported Sensors
- **Oxygen Sensor (O₂)** - Electrochemical sensing using Nernst equation
- **Pressure Sensor** - Piezoresistive pressure measurement
- **Humidity Sensor (RH)** - Temperature-compensated relative humidity
- **Flow Sensor** - Differential pressure flow measurement
- **Thermocouple Type K** - Thermal response measurement

### Signal Analysis
- Time domain waveform display
- Discrete Fourier Transform with 256-point FFT
- S-domain pole-zero analysis for continuous systems
- Z-domain representation with unit circle plot
- Hamming window for spectral leakage reduction

### 3D Visualization
- Interactive 3D model of biomass plant structure
- WebView2-based Three.js rendering engine
- Mouse-controlled camera (rotate, zoom, pan)
- Physically-based rendering with metallic materials
- Real-time lighting and shadow effects

## Requirements

- Windows 10/11 (64-bit)
- .NET Framework 4.7.2 or higher
- Visual Studio 2019 or later
- WebView2 Runtime

### NuGet Dependencies
```
MathNet.Numerics
Microsoft.Web.WebView2
```

## Installation

1. Clone the repository
```bash
git clone https://github.com/yourusername/biomass-plant-monitoring.git
```

2. Open solution in Visual Studio
```bash
cd biomass-plant-monitoring
start ProgramSPS.sln
```

3. Restore NuGet packages and build

## Usage

### Sensor Analysis
1. Select sensor type from dropdown menu
2. Adjust physics parameters using sliders
3. Click "GENERATE SIGNAL" to update all charts
4. Analyze time domain, frequency spectrum, and pole-zero plots

### 3D Viewer
1. Navigate to "3D Model Viewer" tab
2. Use mouse to interact with model:
   - Left click: Rotate
   - Mouse wheel: Zoom
   - Right click: Pan

## Technical Details

### Sensor Physics Models

**Oxygen Sensor (Nernst Equation)**
```
E_O₂ = (R·T / 4F) · ln(0.21 / P_O₂)
```

**Pressure Sensor**
```
P = V_out / S
```

**Humidity Sensor**
```
RH = (P_v / P_sat(T)) × 100%
```

**Flow Sensor (Bernoulli Principle)**
```
Q = A · √(2ΔP / ρ)
```

**Thermocouple Type K**
```
T = Temperature (°C) + dynamic variations
```

### Signal Processing Pipeline
1. Physics-based signal generation at 100 Hz sampling rate
2. Dynamic parameter modulation with sinusoidal variations
3. Additive noise for realistic sensor behavior
4. 200-sample buffer for time domain analysis
5. 256-point FFT with Hamming window for frequency analysis
6. Pole-zero calculation for system stability analysis

### Chart Specifications

| Chart | Type | Purpose |
|-------|------|---------|
| Time Domain | Line | Raw sensor output |
| Frequency (DFT) | Column | Spectral analysis |
| S-Domain | Scatter | Continuous system poles |
| Z-Domain | Scatter | Digital filter representation |

## Architecture

The application uses a modular architecture with separate components for:
- Sensor model computation
- Signal buffer management
- Chart rendering and updates
- 3D scene management
- Parameter control interface

Signal generation occurs in the main thread with immediate UI updates. The 3D viewer runs independently using WebView2's internal rendering pipeline.

## Customization

### Adding New Sensors
1. Add sensor name to `sensorNames` array
2. Define formula in `formulas` array
3. Implement physics model in `GenerateSignal()` method
4. Configure parameter ranges in `paramRanges` array
5. Add parameter labels to `paramLabels` array

### Modifying 3D Model
Edit `GetThreeJsHTML()` method to customize geometry, materials, lighting, or camera settings.

## Contributing

Contributions are welcome. Please follow standard Git workflow:
1. Fork the repository
2. Create a feature branch
3. Commit changes with clear messages
4. Submit pull request with description

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Contact

Project Link: https://github.com/danzzha/biomass-plant-combusting-monitoring

## Acknowledgments

- MathNet.Numerics for mathematical operations
- Three.js for 3D graphics
- Microsoft WebView2 for web content rendering
