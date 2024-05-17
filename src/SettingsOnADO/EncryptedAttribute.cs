namespace SettingsOnADO;


/// <summary>
/// Specifies that a property should be encrypted.
/// </summary>
/// <remarks>
/// This attribute can only be applied to properties of type string.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}
