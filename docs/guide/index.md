# Getting started

## 1. Prepare the robot

Use a Pololu Romi with its Raspberry Pi and Grove board. Enable I2C on the Pi
and check that your user can access I2C and GPIO. Run hardware examples on the Pi.
The documentation build itself does not need a robot.

The library targets .NET 8 and ARM64. Install the .NET 8 SDK on your development
machine and a compatible .NET runtime on the Pi.

## 2. Create a C# application

Run these commands in a new project folder:

```shell
dotnet new console --framework net8.0 --name MyRobot
cd MyRobot
dotnet add package Avans.StatisticalRobot
```

When developing the library from this repository, you can use the existing
`StatisticalRobot-LibTest/RobotProject.csproj` sample instead. It references the
library project directly and is a hardware demo, not an automated test suite.

## 3. Connect an LED

Connect a Grove LED to digital port D5. Port D5 is passed as integer `5`.

Replace `Program.cs` with:

```csharp
using Avans.StatisticalRobot;

var blink = new BlinkLed(5, 500);

while (true)
{
    blink.Update();
    Robot.Wait(10);
}
```

`500` is the interval between state changes in milliseconds. Call `Update()`
often so the LED can change state on time. A long wait in the loop delays it.

## 4. Publish and run

Follow [deployment](deployment.md) to publish for the Pi, copy the files, and run
the application. See [devices and update loops](devices.md) for sensor examples.
