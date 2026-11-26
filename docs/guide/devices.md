# Devices and update loops

## Choose a port

| Connection | Argument | Example |
| --- | --- | --- |
| Digital D2, D4, D5, D6 | Digital port number | `new Led(4)` for D4 |
| Analog A0, A1, A2 | Analog channel index | `new LightSensor(0, 250)` for A0 |
| I2C | Device address | `new LCD16x2(0x3E)` |

Match the device to the port type. Connect one device per Grove port. Check the
address of your I2C device before using an example address.

## Read sensors

This example needs a button on D2, an ultrasonic sensor on D6, and a light sensor
on A0:

```csharp
using Avans.StatisticalRobot;

var button = new Button(2);
var distance = new Ultrasonic(6);
var light = new LightSensor(0, 250);

while (true)
{
    Console.WriteLine($"Button: {button.GetState()}");
    Console.WriteLine($"Distance: {distance.GetUltrasoneDistance()} cm");

    int value = light.GetLux();
    if (value >= 0)
        Console.WriteLine($"Light: {value}");

    Robot.Wait(50);
}
```

`LightSensor.GetLux()` currently returns the analog reading without a lux
conversion. It returns `-1` if the read interval has not passed. Keep the last
valid reading or skip that sample.

## Call periodic methods

Call `BlinkLed.Update()` regularly. Call `Buzzer.Beep()` regularly when using its
interval behavior. These methods do not start background timers. Other devices
can limit their own read frequency; check the API return values before using them.

## Use the API reference

The [Robot API](../api/Avans.StatisticalRobot.Robot.yml) contains the shared
motor, battery, encoder, GPIO, and timing methods. The API reference includes
public types and members. Missing descriptions mean the source still needs XML
documentation comments.
