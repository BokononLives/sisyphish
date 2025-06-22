using sisyphish.GoogleCloud.Firestore;

namespace sisyphish.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<string, GoogleCloudFirestoreValue>? AddIfNotNull(this Dictionary<string, GoogleCloudFirestoreValue>? fields, string? key, string? value)
    {
        if (fields != null && key != null && value != null)
        {
            fields.Add(key, new GoogleCloudFirestoreValue { StringValue = value });
        }

        return fields;
    }

    public static Dictionary<string, GoogleCloudFirestoreValue>? AddIfNotNull(this Dictionary<string, GoogleCloudFirestoreValue>? fields, string? key, long? value)
    {
        if (fields != null && key != null && value != null)
        {
            fields.Add(key, new GoogleCloudFirestoreValue { IntegerValue = value });
        }

        return fields;
    }

    public static Dictionary<string, GoogleCloudFirestoreValue>? AddIfNotNull(this Dictionary<string, GoogleCloudFirestoreValue>? fields, string? key, DateTime? value)
    {
        if (fields != null && key != null && value != null)
        {
            fields.Add(key, new GoogleCloudFirestoreValue { TimestampValue = value?.ToUniversalTime().ToString("O") });
        }

        return fields;
    }

    public static Dictionary<string, GoogleCloudFirestoreValue>? AddIfNotNull<T>(this Dictionary<string, GoogleCloudFirestoreValue>? fields, string? key, List<T>? value, Func<T, GoogleCloudFirestoreValue> mapping)
    {
        if (fields != null && key != null && value != null)
        {
            fields.Add(key, new GoogleCloudFirestoreValue { ArrayValue = new GoogleCloudFirestoreArrayValue
            {
                Values = value.Select(v => mapping(v)).ToList()
            } });
        }

        return fields;
    }
}