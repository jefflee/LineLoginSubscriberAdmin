using System.Text.Json.Serialization;

public partial class LINELoginProfile
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("pictureUrl")]
    public Uri PictureUrl { get; set; }

    [JsonPropertyName("statusMessage")]
    public string StatusMessage { get; set; }
}
