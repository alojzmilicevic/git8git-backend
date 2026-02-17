using Amazon.DynamoDBv2.DataModel;
using core.Storage.Dynamo.Converters;

namespace core.Users.Models;

[DynamoDBTable("Users")]
public class User
{
    [DynamoDBHashKey]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Username { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string? Email { get; set; }

    [DynamoDBProperty]
    public string? AvatarUrl { get; set; }

    [DynamoDBProperty]
    public string? PasswordHash { get; set; }

    [DynamoDBProperty]
    public string AccessToken { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string? RefreshToken { get; set; }

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset CreatedAt { get; set; }

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset UpdatedAt { get; set; }

    [DynamoDBVersion]
    public int? Version { get; set; }
}
