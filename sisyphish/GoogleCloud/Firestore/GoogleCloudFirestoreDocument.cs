namespace sisyphish.GoogleCloud.Firestore;

public class GoogleCloudFirestoreDocument
{
    public string? Name { get; set; }
    public Dictionary<string, GoogleCloudFirestoreValue> Fields { get; set; } = default!;
    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }
    public string? Id => Name?.Split('/')[^1];

    public string? GetString(string key) => Fields.TryGetValue(key, out var value) ? value.StringValue : null;
    public long? GetLong(string key) => Fields.TryGetValue(key, out var value) ? long.TryParse(value.IntegerValue, out var result) ? result : null : null;
    public DateTime? GetTimestamp(string key) => ParseDateTime(Fields.TryGetValue(key, out var value) ? value.TimestampValue : null);
    
    public T? GetEnum<T>(string key) where T : struct, Enum
    {
        var rawValue = GetString(key);

        if (string.IsNullOrEmpty(rawValue) || !Enum.TryParse<T>(rawValue, out var result))
        {
            return default;
        }

        return result;
    }

    public List<GoogleCloudFirestoreDocument> GetList(string key)
    {
        if (!Fields.TryGetValue(key, out var field) || field.ArrayValue?.Values == null)
        {
            return [];
        }

        return field.ArrayValue.Values
            .Select(v => new GoogleCloudFirestoreDocument
            {
                Fields = v.MapValue?.Fields ?? []
            })
            .ToList();
    }
    
    public static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }
}

public class GoogleCloudFirestoreValue
{
    public string? StringValue { get; set; }
    public string? IntegerValue { get; set; }
    public bool? BooleanValue { get; set; }
    public double? DoubleValue { get; set; }
    public string? TimestampValue { get; set; }
    public GoogleCloudFirestoreArrayValue? ArrayValue { get; set; }
    public GoogleCloudFirestoreMapValue? MapValue { get; set; }
}

public class GoogleCloudFirestoreArrayValue
{
    public List<GoogleCloudFirestoreValue>? Values { get; set; }
}

public class GoogleCloudFirestoreMapValue
{
    public Dictionary<string, GoogleCloudFirestoreValue>? Fields { get; set; }
}

public class GoogleCloudFirestoreQueryRequest
{
    public GoogleCloudFirestoreStructuredQuery? StructuredQuery { get; set; }
}

public class GoogleCloudFirestoreStructuredQuery
{
    public List<GoogleCloudFirestoreCollectionSelector> From { get; set; } = [];
    public GoogleCloudFirestoreWhereClause? Where { get; set; }
}

public class GoogleCloudFirestoreCollectionSelector
{
    public string? CollectionId { get; set; }
}

public class GoogleCloudFirestoreWhereClause
{
    public GoogleCloudFirestoreCompositeFilter? CompositeFilter { get; set; }
    public GoogleCloudFirestoreFieldFilter? FieldFilter { get; set; }
}

public class GoogleCloudFirestoreFieldFilter
{
    public GoogleCloudFirestoreFieldReference? Field { get; set; }
    public string? Op { get; set; }
    public GoogleCloudFirestoreValue? Value { get; set; }
}

public class GoogleCloudFirestoreFieldReference
{
    public string? FieldPath { get; set; }
}

public class GoogleCloudFirestoreQueryResponse
{
    public GoogleCloudFirestoreDocument? Document { get; set; }
}

public class GoogleCloudFirestoreCompositeFilter
{
    public string? Op { get; set; }
    public List<GoogleCloudFirestoreFieldFilter> Filters { get; set; } = [];
}