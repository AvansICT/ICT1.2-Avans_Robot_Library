# Deployment and troubleshooting

## Publish for the Raspberry Pi

From your application folder, run:

```shell
dotnet publish -c Release -r linux-arm64 --no-self-contained
```

For the sample in this repository, run from the repository root:

```shell
dotnet publish StatisticalRobot-LibTest/RobotProject.csproj -c Release -r linux-arm64 --no-self-contained
```

Copy all files in `bin/Release/net8.0/linux-arm64/publish/` from the application
folder to the Pi. On the Pi, run the application DLL:

```shell
dotnet MyRobot.dll
```

Use your application's actual assembly name. For the repository sample it is
`RobotProject.dll`. The Pi needs a compatible .NET runtime because this publish
command does not include one.

## Troubleshooting

| Problem | Check |
| --- | --- |
| I2C or GPIO access fails | Run on the Pi; check I2C configuration, user permissions, wiring, and board seating. |
| LED does not blink | Check the digital port and call `Update()` often. |
| Light reading is `-1` | Wait for the configured read interval before expecting a new sample. |
| Sensor values seem wrong | Check the port type, wiring, sensor return format, and polling interval. |
| Pi reboots or I2C becomes unstable | Check and recharge the robot batteries. |

Use `Robot.ReadBatteryMillivolts()` to inspect battery voltage. The repository's
hardware notes report instability around 5600 mV and repeated Pi restarts below
5500 mV; these are observations for this setup, not guaranteed thresholds.
