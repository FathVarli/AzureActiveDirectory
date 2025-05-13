using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph.Models;

namespace AzureExternalDirectory.Infrastructure.GraphService.Helper
{
    public static class DirectoryObjectExtensions
    {
        private static readonly AttributeSearchService SearchService = new AttributeSearchService(
            new NullLogger<AttributeSearchService>());

        private static object GetAttributeValue<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            return SearchService.GetAttributeValue(directoryObject, attributeName);
        }

        private static TValue GetAttributeValue<T, TValue>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            var value = directoryObject.GetAttributeValue(attributeName);
            if (value == null)
                return default(TValue);

            try
            {
                return (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            catch
            {
                return default(TValue);
            }
        }

        public static string GetStringAttribute<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            return directoryObject.GetAttributeValue<T, string>(attributeName);
        }

        public static bool GetBooleanAttribute<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            return directoryObject.GetAttributeValue<T, bool>(attributeName);
        }

        public static int GetIntAttribute<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            return directoryObject.GetAttributeValue<T, int>(attributeName);
        }

        public static DateTime? GetDateTimeAttribute<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            var value = directoryObject.GetAttributeValue(attributeName);
            if (value == null)
                return null;

            if (value is DateTime dateTime)
                return dateTime;

            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed;

            return null;
        }

        public static List<string> GetListAttribute<T>(this T directoryObject, string attributeName)
            where T : DirectoryObject
        {
            var value = directoryObject.GetAttributeValue(attributeName);
            if (value == null)
                return new List<string>();

            if (value is List<string> stringList)
                return stringList;

            if (value is IEnumerable<object> enumerable)
                return enumerable.Select(x => x?.ToString()).Where(x => x != null).ToList();

            if (value is string str)
                return new List<string> { str };

            return new List<string>();
        }

        public static Dictionary<string, object> ToDictionary(this User user)
        {
            var result = new Dictionary<string, object>();

            // Get all supported field names from deserializers
            var fieldDeserializers = user.GetFieldDeserializers();

            // Get all properties of User class
            var properties = typeof(User).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create a lookup for faster property matching
            var propertyLookup = properties.ToLookup(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var fieldName in fieldDeserializers.Keys)
            {
                var matchingProperty = propertyLookup[fieldName].FirstOrDefault();

                if (matchingProperty != null)
                {
                    try
                    {
                        var value = matchingProperty.GetValue(user);
                        if (value != null)
                        {
                            result[fieldName] = value;
                        }
                    }
                    catch (Exception)
                    {
                        // Skip properties that can't be read
                        continue;
                    }
                }
            }

            // Add AdditionalData
            foreach (var kvp in user.AdditionalData)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        public static Dictionary<string, object> ToDictionary(this Group group)
        {
            var result = new Dictionary<string, object>();

            var fieldDeserializers = group.GetFieldDeserializers();
            var properties = typeof(Group).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyLookup = properties.ToLookup(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var fieldName in fieldDeserializers.Keys)
            {
                var matchingProperty = propertyLookup[fieldName].FirstOrDefault();

                if (matchingProperty == null) 
                    continue;
                try
                {
                    var value = matchingProperty.GetValue(group);
                    if (value != null)
                    {
                        result[fieldName] = value;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            foreach (var kvp in group.AdditionalData)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }
}