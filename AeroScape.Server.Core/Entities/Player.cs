namespace AeroScape.Server.Core.Entities;

public class Player
{
    public int Index { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Rights { get; set; }
    // Core game state attributes to be expanded...
}