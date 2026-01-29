using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace core.Storage.Dynamo.Converters;

public class TimeSpanConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is TimeSpan ts)
        {
            return new Primitive(ts.Ticks.ToString(), true);
        }
        return DynamoDBNull.Null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is Primitive primitive && primitive.Value != null)
        {
            if (long.TryParse(primitive.AsString(), out var ticks))
            {
                return TimeSpan.FromTicks(ticks);
            }
        }
        return TimeSpan.Zero;
    }
}
