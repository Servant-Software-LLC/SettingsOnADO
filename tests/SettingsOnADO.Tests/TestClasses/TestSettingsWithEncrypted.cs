namespace SettingsOnADO.Tests.TestClasses;

internal class TestSettingsWithEncrypted
{
    public int Id { get; set; }
    public string? Name { get; set; }

    [Encrypted]
    public string? Password { get; set; }
}
