namespace Avans.StatisticalRobot.Interfaces;

/// <summary>Defines a component that needs regular updates from the application loop.</summary>
public interface IUpdatable
{
    /// <summary>Updates the component's state; call this method regularly.</summary>
    public void Update();
}
