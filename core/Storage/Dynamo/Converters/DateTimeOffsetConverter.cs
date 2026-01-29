using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace core.Storage.Dynamo.Converters;

public class DateTimeOffsetConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is DateTimeOffset dto)
        {
            return new Primitive(dto.ToString("O"));
        }
        return DynamoDBNull.Null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is Primitive primitive && primitive.Value != null)
        {
            return DateTimeOffset.Parse(primitive.AsString());
        }
        return default(DateTimeOffset);
    }
}
