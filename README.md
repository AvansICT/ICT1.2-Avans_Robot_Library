# StatisticalRobot-Lib

Library for controlling the Pololu Romi (with Raspberry Pi) plus Grove accessories in C#. It wraps low-level I2C, GPIO and struct packing logic so you can focus on behavior (motors, sensors, display, feedback loops).

> This repository contains the source code and a sample project. In normal projects you consume the library as a NuGet package.

## 1. Developing

The library is developed using VSCode, but Visual Studio should also work. The C# solution exists of two folders:

- **StatisticalRobot-Lib** -
  This folder contains the actual library code.
- **StatisticalRobot-LibTest** -
  This folder contains a test project to test the library. The project uses the libraries project as reference.

The library code can be developed within the StatisticalRobot-Lib project. The StatisticalRobot-LibTest project can be used to test and debug the library.
When using VSCode, please open this folder as workspace (encapsulating both the library and test projects) for the provided tasks to work properly.

## 2. Target Platform

Run on the Raspberry Pi (linux-arm64) attached to the Romi. Recommended settings:

- `TargetFramework`: net8.0
- `RuntimeIdentifier`: linux-arm64 (optional if you deploy cross-platform code)
- Not self-contained to save space unless offline deployment is required.

## 3. Grove board pins (choosing pin numbers)

Most constructors take a pin argument that refers to the Grove board ports on the Romi’s hat:

- Digital devices (Led, BlinkLed, Button, Ultrasonic, DHT11, InfraredReflective, PIRMotion, Buzzer):
  - Use the D port number as an integer.
  - Examples: D2 ⇒ 2, D3 ⇒ 3, D4 ⇒ 4, D5 ⇒ 5, D6 ⇒ 6.
  - e.g., `new Led(4)` means the device is connected to Grove port D4.
- Analog devices (LightSensor):
  - Use the analog channel index as a byte.
  - Examples: A0 ⇒ 0, A1 ⇒ 1, A2 ⇒ 2.
  - e.g., `new LightSensor(0, 250)` reads from A0 every 250 ms.
- I2C devices (LCD16x2):
  - Use the device’s I2C address (often 0x3E for 16x2 LCDs).
  - e.g., `new LCD16x2(0x3E)`.

Tips

- Don’t connect two devices to the same Grove port simultaneously.
- Match device type to port type: digital devices on Dn, analog devices on An, I2C devices on the I2C port.
- The examples in this repo use: D4 (Led), D5 (BlinkLed), D2 (Button), D6 (Ultrasonic), D3 (DHT11), A0 (LightSensor), and I2C 0x3E (LCD16x2).

## 4. Quick Start

```csharp
using Avans.StatisticalRobot;

// Blink an LED on pin 5 every 500 ms
BlinkLed blink = new BlinkLed(5, 500);
Led led = new Led(4);
Button btn = new Button(2);
Ultrasonic ultra = new Ultrasonic(6);
DHT11 climate = new DHT11(3);
LightSensor light = new LightSensor(0, 250); // analog A0, reads every 250 ms
LCD16x2 lcd = new LCD16x2(0x3E);
lcd.SetText("Hello Romi!");

// One‑off actions
led.SetOn();
Robot.Wait(500);
led.SetOff();
Robot.PlayNotes("o4l16ceg>c");
Robot.Motors(100, 100); Robot.Wait(800); Robot.Motors(0, 0);

while (true)
{
    blink.Update(); // must be called periodically

    Console.WriteLine($"Button: {btn.GetState()}");
    Console.WriteLine($"Distance: {ultra.GetUltrasoneDistance()} cm");
    int lux = light.GetLux();
    if (lux >= 0) Console.WriteLine($"Lux: {lux}"); // -1 means no new sample yet

    int[] tAndH = climate.GetTemperatureAndHumidity();
    if (tAndH[0] != 0 || tAndH[2] != 0)
        Console.WriteLine($"Temp: {tAndH[2]}°C Humidity: {tAndH[0]}%");

    bool[] romiButtons = Robot.ReadButtons();
    bool a = romiButtons.Length > 0 && romiButtons[0];
    bool b = romiButtons.Length > 1 && romiButtons[1];
    bool c = romiButtons.Length > 2 && romiButtons[2];
    Console.WriteLine($"Romi Buttons: A={a} B={b} C={c}");

    Console.WriteLine($"Battery: {Robot.ReadBatteryMillivolts()} mV");
    short[] enc = Robot.ReadEncoders();
    Console.WriteLine($"Encoders L={enc[0]} R={enc[1]}");

    Robot.Wait(1000); // simple pacing
}
```

## 5. Device Classes & API Overview

All classes live in namespace `Avans.StatisticalRobot`.

| Device                    | Constructor                                         | Key Methods                                                                                                                                                                                                                         |
| ------------------------- | --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Led`                     | `Led(int pin)`                                      | `SetOn()`, `SetOff()`                                                                                                                                                                                                               |
| `BlinkLed`                | `BlinkLed(int pin, int msBlink)`                    | `Update()` (call periodically; toggles state based on interval)                                                                                                                                                                     |
| `Buzzer`                  | `Buzzer(int pin, int intervalms)`                   | `Beep()` (call periodically; toggles on/off based on interval)                                                                                                                                                                      |
| `Button`                  | `Button(int pin)`                                   | `GetState()` returns `string` ("Released"/"Pressed")                                                                                                                                                                                |
| `Ultrasonic`              | `Ultrasonic(int pin)`                               | `GetUltrasoneDistance()` returns `int` (cm)                                                                                                                                                                                         |
| `DHT11`                   | `DHT11(int pin)`                                    | `GetTemperatureAndHumidity()` returns `int[5]` (humidity int, humidity dec, temp int, temp dec, checksum)                                                                                                                           |
| `LightSensor`             | `LightSensor(byte analogPin, int intervalms)`       | `GetLux()` returns `int` value, or `-1` if the read interval hasn’t elapsed                                                                                                                                                         |
| `PIRMotion`               | `PIRMotion(int pin, int intervalms, int alerttime)` | `Watch()` returns `1` (motion), `0` (no motion), or `-1` (no new sample yet)                                                                                                                                                        |
| `InfraredReflective`      | `InfraredReflective(int pin)`                       | `Watch()` returns `1` (detect), `0` (no detect), or `-1` (no new sample yet)                                                                                                                                                        |
| `LCD16x2`                 | `LCD16x2(byte i2cAddress)`                          | `SetText(string text)`                                                                                                                                                                                                              |
| (Internal) `Robot` static | —                                                   | `Motors(short left, short right)`, `PlayNotes(string)`, `LEDs(byte r, byte y, byte g)`, `ReadButtons()`, `ReadBatteryMillivolts()`, `ReadEncoders()`, `AnalogRead(byte)`, `PulseIn(int, PinValue, int)`, `Wait(int)`, `WaitUs(int)` |

### Update Loop Pattern

Components implementing `IUpdatable` (e.g. `BlinkLed`) require you to call `Update()` regularly from your main loop or a timer. This keeps timing logic inside the device class.

### Timing & Intervals

Some sensors (e.g. `LightSensor`) internally rate-limit reads. If `GetLux()` returns `-1`, reuse the last non-negative value or just skip logging.

## 6. Deployment to Romi

Cross-compile or publish directly on the Pi:

```powershell
dotnet publish -c Release -r linux-arm64 --no-self-contained
```

Copy the output folder (DLL + dependencies) to the Pi and run:

```powershell
dotnet YourProject.dll
```

## 7. Troubleshooting

| Symptom                                | Possible Cause                         | Action                                                                        |
| -------------------------------------- | -------------------------------------- | ----------------------------------------------------------------------------- |
| All sensor reads 0                     | Grove Hat not detected / wrong address | Check I2C cabling; ensure hat seated; watch console for version message.      |
| Battery exceptions / random I2C errors | Low voltage (< 5600 mV)                | Replace or recharge batteries; monitor using `Robot.ReadBatteryMillivolts()`. |
| Raspberry Pi reboots in loop           | Voltage < 5500 mV                      | Immediately power off and recharge batteries.                                 |
| DHT11 inconsistent values              | Too frequent polling                   | Add delay (≥ 1000 ms) between reads.                                          |
| BlinkLed not blinking                  | Forgot `Update()` call                 | Ensure loop calls `Update()` periodically.                                    |

## 8. Notes on Voltage

Around `5600 mV` the robot may become unstable (I2C exceptions). Below `5500 mV` the Pi can brown out and reboot repeatedly (Romi startup sound loop). Six 2600mAh cells gave roughly 7h of intermittent development time; continuous runtime not yet measured.

## 9. Contributing (Internal)

For maintainers: use the test project for rapid iteration. Publish updates to the private NuGet repository.

### Building from Source (Maintainers)

```powershell
dotnet build
dotnet publish StatisticalRobot-Lib/StatisticalRobot-Lib.csproj -c Release -r linux-arm64 --no-self-contained
```
