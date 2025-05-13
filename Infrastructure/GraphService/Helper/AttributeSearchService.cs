using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace AzureExternalDirectory.Infrastructure.GraphService.Helper
{
    public class AttributeSearchService
{
    private readonly ILogger<AttributeSearchService> _logger;

    public AttributeSearchService(ILogger<AttributeSearchService> logger)
    {
        _logger = logger;
    }

    public object GetAttributeValue(object sourceObject, string attributeName)
    {
        if (sourceObject == null || string.IsNullOrEmpty(attributeName))
            return null;

        // Convert object to dictionary for easier searching
        var data = ConvertToSearchableDictionary(sourceObject);
        
        // Search for the attribute (case-insensitive)
        return FindAttribute(data, attributeName);
    }

    private Dictionary<string, object> ConvertToSearchableDictionary(object sourceObject)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        
        if (sourceObject == null)
            return result;

        var objectType = sourceObject.GetType();
        
        // Add all properties using reflection
        var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(sourceObject);
                if (value != null)
                {
                    // Add both original property name and camelCase version
                    result[prop.Name] = value;
                    result[ConvertToCamelCase(prop.Name)] = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting property {prop.Name}: {ex.Message}");
            }
        }

        // Handle AdditionalData for Azure Graph objects
        if (sourceObject is User azureUser)
        {
            foreach (var item in azureUser.AdditionalData)
            {
                result[item.Key] = item.Value;
            }
        }
        else if (sourceObject is Group azureGroup)
        {
            foreach (var item in azureGroup.AdditionalData)
            {
                result[item.Key] = item.Value;
            }
        }
        // Add support for other directory objects if needed
        else if (sourceObject is DirectoryObject directoryObject)
        {
            foreach (var item in directoryObject.AdditionalData)
            {
                result[item.Key] = item.Value;
            }
        }

        return result;
    }

    private object FindAttribute(Dictionary<string, object> data, string attributeName)
    {
        // Direct search (case-insensitive due to StringComparer.OrdinalIgnoreCase)
        if (data.TryGetValue(attributeName, out var attribute))
        {
            return attribute;
        }

        // If not found, log available attributes for debugging
        _logger.LogDebug($"Attribute '{attributeName}' not found. Available attributes: {string.Join(", ", data.Keys)}");
        
        return null;
    }

    private static string ConvertToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
}