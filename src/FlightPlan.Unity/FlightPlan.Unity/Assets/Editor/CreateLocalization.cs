/* Flight Plan
 * Copyright (C) 2024  schlosrat
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace FlightPlan.Unity.Editor
{
    public static class CreateLocalization
    {
        private static readonly Dictionary<string, string> Replacements = new()
        {
            {"°", "deg"},
            {"Δ", "d"}
        };

        [MenuItem("Assets/Create Localization")]
        private static void CreateLocalizationFromUXML()
        {
            // Load the XML document
            var doc = new XmlDocument();
            doc.Load("Assets/UI/FP_UI.uxml");

            // Create a Dictionary for CSV content
            var csvContent = new Dictionary<string, string>();

            // Call the recursive method for the root element
            ProcessNode(doc.DocumentElement, csvContent);

            // Save the modified XML document
            doc.Save("Assets/UI/FP_UI.uxml");

            // Write the CSV content to a file
            File.WriteAllLines(
                "Assets/FlightPlanKeys.csv",
                csvContent.Select(kvp => $"{kvp.Key},text,,\"{kvp.Value}\"").Prepend("\"Key\",\"Type\",\"Desc\",\"English\"")
            );
        }

        private static void ProcessNode(XmlNode node, Dictionary<string, string> csvContent)
        {
            // If the element is a ui:TextField
            if (node.Name != "ui:TextField")
            {
                // Check if it has a text or label attribute
                var attr = node.Attributes!["text"] ?? node.Attributes!["label"];
                if (attr != null)
                {
                    // Replace the value with a localization key
                    var originalText = attr.Value;

                    if (Regex.Match(originalText, @"^(\d|#)").Success)
                    {
                        return;
                    }

                    var keyName = SanitizeKeyName(node.Attributes["name"]?.Value ?? originalText);
                    if (keyName.All(c => c == '_'))
                    {
                        return;
                    }

                    var localizationKey = $"FlightPlan/UI/{keyName}";

                    // Check if the key already exists
                    if (!csvContent.TryAdd(localizationKey, originalText))
                    {
                        // If the key exists and the value is different, create a new key
                        if (csvContent[localizationKey] != originalText)
                        {
                            localizationKey = $"{localizationKey}_{SanitizeKeyName(originalText)}";
                            csvContent[localizationKey] = originalText;
                        }
                    }

                    attr.Value = $"#{localizationKey}";
                }
            }

            // Iterate over all child nodes
            foreach (XmlNode childNode in node.ChildNodes)
            {
                ProcessNode(childNode, csvContent);
            }
        }

        private static string SanitizeKeyName(string keyName)
        {
            var sanitizedKeyName = keyName;
            foreach (var (key, value) in Replacements)
            {
                sanitizedKeyName = sanitizedKeyName.Replace(key, value);
            }

            sanitizedKeyName = Regex.Replace(sanitizedKeyName, "[^a-zA-Z0-9_]", "");
            return sanitizedKeyName;
        }
    }
}