using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace core.Storage.Dynamo.Converters;

public class EnumConverter<T> : IPropertyConverter where T : struct, Enum
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is T enumValue)
        {
            return new Primitive(enumValue.ToString());
        }
        return DynamoDBNull.Null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is Primitive primitive && primitive.Value != null)
        {
            if (Enum.TryParse<T>(primitive.AsString(), out var result))
            {
                return result;
            }
        }
        return default(T);
    }
}
