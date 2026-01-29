using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace core.Storage.Dynamo.Converters;

public class IgnoreDefaultConverter<T> : IPropertyConverter where T : struct
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value == null)
        {
            return DynamoDBNull.Null;
        }

        var defaultValue = default(T);
        if (EqualityComparer<T>.Default.Equals((T)value, defaultValue))
        {
            return DynamoDBNull.Null;
        }

        if (value is int intValue)
            return new Primitive(intValue.ToString(), true);
        if (value is long longValue)
            return new Primitive(longValue.ToString(), true);
        if (value is double doubleValue)
            return new Primitive(doubleValue.ToString(), true);
        if (value is decimal decimalValue)
            return new Primitive(decimalValue.ToString(), true);
        if (value is bool boolValue)
            return new DynamoDBBool(boolValue);

        return new Primitive(value.ToString());
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry == null || entry is DynamoDBNull)
        {
            return default(T)!;
        }

        if (entry is Primitive primitive)
        {
            var type = typeof(T);
            if (type == typeof(int)) return int.Parse(primitive.AsString());
            if (type == typeof(long)) return long.Parse(primitive.AsString());
            if (type == typeof(double)) return double.Parse(primitive.AsString());
            if (type == typeof(decimal)) return decimal.Parse(primitive.AsString());
            if (type == typeof(bool)) return bool.Parse(primitive.AsString());
        }

        if (entry is DynamoDBBool boolEntry)
        {
            return boolEntry.Value;
        }

        return default(T)!;
    }
}
