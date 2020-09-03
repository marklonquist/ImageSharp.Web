// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SixLabors.ImageSharp.Web.Commands.Converters
{
    /// <summary>
    /// Allows the conversion of strings into rgba32 pixel colors.
    /// </summary>
    internal class ColorConverter : ICommandConverter
    {
        /// <summary>
        /// The web color hexadecimal regex. Matches strings arranged
        /// in rgb, rgba, rrggbb, or rrggbbaa format to match web syntax.
        /// </summary>
        private static readonly Regex HexColorRegex = new Regex("([0-9a-fA-F]{3}){1,2}", RegexOptions.Compiled);

        /// <summary>
        /// The number color regex.
        /// </summary>
        private static readonly Regex NumberRegex = new Regex(@"\d+", RegexOptions.Compiled);

        /// <summary>
        /// The color constants table map.
        /// </summary>
        private static readonly Lazy<IDictionary<string, Color>> ColorConstantsTable = new Lazy<IDictionary<string, Color>>(InitializeColorConstantsTable);

        /// <inheritdoc/>
        public Type Type => typeof(Color);

        /// <inheritdoc/>
        public object ConvertFrom(CommandParser parser, CultureInfo culture, string value, Type propertyType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default(Color);
            }

            // Numeric r,g,b - r,g,b,a
            char separator = culture.TextInfo.ListSeparator[0];

            if (value.IndexOf(separator) != -1)
            {
                string[] components = value.Split(separator);

                bool convert = true;
                foreach (string component in components)
                {
                    if (!NumberRegex.IsMatch(component))
                    {
                        convert = false;
                    }
                }

                if (convert)
                {
                    List<byte> rgba = parser.ParseValue<List<byte>>(value, culture);

                    return rgba.Count switch
                    {
                        4 => Color.FromRgba(rgba[0], rgba[1], rgba[2], rgba[3]),
                        3 => Color.FromRgb(rgba[0], rgba[1], rgba[2]),
                        _ => default,
                    };
                }
            }

            // Hex colors rgb, rrggbb, rrggbbaa
            if (HexColorRegex.IsMatch(value))
            {
                return Color.ParseHex(value);
            }

            // Named colors
            IDictionary<string, Color> table = ColorConstantsTable.Value;
            return table.ContainsKey(value) ? table[value] : default;
        }

        /// <summary>
        /// Initializes color table mapping color constants.
        /// </summary>
        /// <returns>The <see cref="IDictionary{String, Color}"/>.</returns>
        private static IDictionary<string, Color> InitializeColorConstantsTable()
        {
            IDictionary<string, Color> table = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

            foreach (FieldInfo field in typeof(Color).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(Color))
                {
                    table[field.Name] = (Color)field.GetValue(null);
                }
            }

            return table;
        }
    }
}
