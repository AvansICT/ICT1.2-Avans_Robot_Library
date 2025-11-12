using Avans.StatisticalRobot;
// --- Initialisatie van sensoren en actuatoren met expliciete types ---
// De pinnen (bv. 5) en I2C adressen (bv. 0x3E) zijn voorbeelden en moeten overeenkomen met hoe de hardware is aangesloten.

// Voorbeeld: LED laten knipperen (actuator)
// Deze klasse implementeert IUpdatable, wat betekent dat de Update() methode periodiek aangeroepen moet worden in de hoofdlus.
// Parameters: (pin, interval in milliseconden)
BlinkLed knipperLed = new BlinkLed(5, 500);

// Voorbeeld: Standaard LED (actuator)
// Een simpele LED die aan of uit gezet kan worden.
// Parameters: (pin)
Led led = new Led(4);

// Voorbeeld: Knop (sensor)
// Leest de status van een digitale knop.
// Parameters: (pin, standaardWaardeHoog) - standaardWaardeHoog is optioneel.
Button button = new Button(2);

// Voorbeeld: Ultrasone afstandssensor (sensor)
// Meet de afstand in centimeters.
// Parameters: (pin)
Ultrasonic ultrasonic = new Ultrasonic(6);

// Voorbeeld: DHT11 temperatuur- en luchtvochtigheidssensor (sensor)
// Leest de temperatuur en luchtvochtigheid.
// Parameters: (pin)
DHT11 dht11 = new DHT11(3);

// Voorbeeld: Lichtsensor (analog sensor)
// Leest een analoge waarde van de lichtsensor. De pin moet een analoge pin zijn (A0, A1, etc.).
// Parameters: (analogPin, intervalms)
// intervalms bepaalt hoe vaak (ms) de sensor daadwerkelijk leest; choose a sensible default like 250 ms.
LightSensor lightSensor = new LightSensor(0, 250); // A0

// Voorbeeld: 16x2 LCD-scherm (actuator, I2C)
// Toont tekst op een LCD-scherm.
// Parameters: (i2cAdres)
LCD16x2 lcd = new LCD16x2(0x3E);
lcd.SetText("Hallo, Robot!");

// Eenmalige actie: zet de LED aan, wacht een halve seconde en zet hem weer uit.
led.SetOn();
Robot.Wait(500); // Wacht 500 milliseconden
led.SetOff();

// Eenmalige actie: speel een deuntje op de Romi's ingebouwde buzzer.
Robot.PlayNotes("o4l16ceg>c");

// Eenmalige actie: laat de robot een klein stukje vooruit rijden en dan stoppen.
Robot.Motors(100, 100);
Robot.Wait(1000);
Robot.Motors(0, 0);

// De hoofdlus van het programma, deze blijft zich herhalen.
while (true)
{
    // De Update() methode van knipperLed wordt aangeroepen om de LED te laten knipperen.
    knipperLed.Update();

    // Lees de status van de knop en print deze naar de console.
    Console.WriteLine($"Knop status: {button.GetState()}");
    Console.WriteLine($"Knop status: {button.GetState()}"); // Geeft een PinValue terug (High/Low)

    // Lees de afstand van de ultrasone sensor en print deze.
    Console.WriteLine($"Afstand: {ultrasonic.GetUltrasoneDistance()} cm");

    // Lees de waarde van de lichtsensor en print deze (GetLux() retourneert -1 als er geen nieuwe waarde is).
    Console.WriteLine($"Lichtwaarde: {lightSensor.GetLux()}");

    // Lees de temperatuur en luchtvochtigheid van de DHT11 sensor.
    int[] dhtData = dht11.GetTemperatureAndHumidity();
    if (dhtData[0] != 0 || dhtData[2] != 0) // Controleer of de data valide is
    {
        Console.WriteLine($"Temperatuur: {dhtData[2]}°C, Luchtvochtigheid: {dhtData[0]}%");
    }

    // Lees de ingebouwde sensoren van de Romi robot.
    Console.WriteLine($"Batterijspanning: {Robot.ReadBatteryMillivolts()} mV");
    bool[] romiButtons = Robot.ReadButtons();
    // Gebruik veilige indexering in plaats van ternary expressies
    bool aButton = romiButtons.Length > 0 && romiButtons[0];
    bool bButton = romiButtons.Length > 1 && romiButtons[1];
    bool cButton = romiButtons.Length > 2 && romiButtons[2];
    Console.WriteLine($"Romi Knoppen: A={aButton}, B={bButton}, C={cButton}");
    short[] encoders = Robot.ReadEncoders();
    Console.WriteLine($"Encoders: Links={encoders[0]}, Rechts={encoders[1]}");

    // Wacht 1 seconde voordat de lus opnieuw wordt uitgevoerd.
    Robot.Wait(1000);
}