using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenNefia.Core.UserInterface.XAML
{
    /// <summary>
    /// Provides utilities for working with types at runtime.
    /// </summary>
    public static class TypeUtilities
    {
        private static readonly int[] Conversions =
        {
            0b101111111111101, // Boolean
            0b100001111111110, // Char
            0b101111111111111, // SByte
            0b101111111111111, // Byte
            0b101111111111111, // Int16
            0b101111111111111, // UInt16
            0b101111111111111, // Int32
            0b101111111111111, // UInt32
            0b101111111111111, // Int64
            0b101111111111111, // UInt64
            0b101111111111101, // Single
            0b101111111111101, // Double
            0b101111111111101, // Decimal
            0b110000000000000, // DateTime
            0b111111111111111, // String
        };

        private static readonly int[] ImplicitConversions =
        {
            0b000000000000001, // Boolean
            0b001110111100010, // Char
            0b001110101010100, // SByte
            0b001111111111000, // Byte
            0b001110101010000, // Int16
            0b001111111100000, // UInt16
            0b001110101000000, // Int32
            0b001111110000000, // UInt32
            0b001110100000000, // Int64
            0b001111000000000, // UInt64
            0b000110000000000, // Single
            0b000100000000000, // Double
            0b001000000000000, // Decimal
            0b010000000000000, // DateTime
            0b100000000000000, // String
        };

        private static readonly Type[] InbuiltTypes =
        {
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(string),
        };

        private static readonly Type[] NumericTypes =
        {
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };

        /// <summary>
        /// Returns a value indicating whether null can be assigned to the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type accepts null values; otherwise false.</returns>
        public static bool AcceptsNull(Type type)
        {
            return !type.IsValueType || IsNullableType(type);
        }

        /// <summary>
        /// Returns a value indicating whether null can be assigned to the specified type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>True if the type accepts null values; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AcceptsNull<T>()
        {
            return default(T) is null;
        }

        /// <summary>
        /// Returns a value indicating whether value can be casted to the specified type.
        /// If value is null, checks if instances of that type can be null.
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="value">The value to check if cast possible</param>
        /// <returns>True if the cast is possible, otherwise false.</returns>
        public static bool CanCast<T>(object value)
        {
            return value is T || value is null && AcceptsNull<T>();
        }

        /// <summary>
        /// Try to convert a value to a type by any means possible.
        /// </summary>
        /// <param name="to">The type to convert to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The culture to use.</param>
        /// <param name="result">If successful, contains the convert value.</param>
        /// <returns>True if the cast was successful, otherwise false.</returns>
        public static bool TryConvert(Type to, object value, CultureInfo culture, out object? result)
        {
            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            var toUnderl = Nullable.GetUnderlyingType(to) ?? to;
            var from = value.GetType();

            if (toUnderl.IsAssignableFrom(from))
            {
                result = value;
                return true;
            }

            if (toUnderl == typeof(string))
            {
                result = Convert.ToString(value, culture);
                return true;
            }

            if (toUnderl.IsEnum && from == typeof(string))
            {
                if (Enum.IsDefined(toUnderl, (string)value))
                {
                    result = Enum.Parse(toUnderl, (string)value);
                    return true;
                }
            }

            if (!from.IsEnum && toUnderl.IsEnum)
            {
                result = null;

                if (TryConvert(Enum.GetUnderlyingType(toUnderl), value, culture, out var enumValue))
                {
                    result = Enum.ToObject(toUnderl, enumValue!);
                    return true;
                }
            }

            if (from.IsEnum && IsNumeric(toUnderl))
            {
                try
                {
                    result = Convert.ChangeType((int)value, toUnderl, culture);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            var convertableFrom = Array.IndexOf(InbuiltTypes, from);
            var convertableTo = Array.IndexOf(InbuiltTypes, toUnderl);

            if (convertableFrom != -1 && convertableTo != -1)
            {
                if ((Conversions[convertableFrom] & 1 << convertableTo) != 0)
                {
                    try
                    {
                        result = Convert.ChangeType(value, toUnderl, culture);
                        return true;
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
            }

            var toTypeConverter = TypeDescriptor.GetConverter(toUnderl);

            if (toTypeConverter.CanConvertFrom(from) == true)
            {
                result = toTypeConverter.ConvertFrom(null, culture, value);
                return true;
            }

            var fromTypeConverter = TypeDescriptor.GetConverter(from);

            if (fromTypeConverter.CanConvertTo(toUnderl) == true)
            {
                result = fromTypeConverter.ConvertTo(null, culture, value, toUnderl);
                return true;
            }

            var cast = FindTypeConversionOperatorMethod(from, toUnderl, OperatorType.Implicit | OperatorType.Explicit);

            if (cast != null)
            {
                result = cast.Invoke(null, new[] { value });
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Try to convert a value to a type using the implicit conversions allowed by the C#
        /// language.
        /// </summary>
        /// <param name="to">The type to convert to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">If successful, contains the converted value.</param>
        /// <returns>True if the convert was successful, otherwise false.</returns>
        public static bool TryConvertImplicit(Type to, object value, out object? result)
        {
            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            var from = value.GetType();

            if (to.IsAssignableFrom(from))
            {
                result = value;
                return true;
            }

            var convertableFrom = Array.IndexOf(InbuiltTypes, from);
            var convertableTo = Array.IndexOf(InbuiltTypes, to);

            if (convertableFrom != -1 && convertableTo != -1)
            {
                if ((ImplicitConversions[convertableFrom] & 1 << convertableTo) != 0)
                {
                    try
                    {
                        result = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
            }

            var cast = FindTypeConversionOperatorMethod(from, to, OperatorType.Implicit);

            if (cast != null)
            {
                result = cast.Invoke(null, new[] { value });
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Convert a value to a type by any means possible, returning the default for that type
        /// if the value could not be converted.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The type to convert to..</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object? ConvertOrDefault(object value, Type type, CultureInfo culture)
        {
            return TryConvert(type, value, culture, out var result) ? result : Default(type);
        }

        /// <summary>
        /// Convert a value to a type using the implicit conversions allowed by the C# language or
        /// return the default for the type if the value could not be converted.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The type to convert to.</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object? ConvertImplicitOrDefault(object value, Type type)
        {
            return TryConvertImplicit(type, value, out var result) ? result : Default(type);
        }

        public static T ConvertImplicit<T>(object value)
        {
            if (TryConvertImplicit(typeof(T), value, out var result))
            {
                return (T)result!;
            }

            throw new InvalidCastException(
                $"Unable to convert object '{value ?? "(null)"}' of type '{value?.GetType()}' to type '{typeof(T)}'.");
        }

        /// <summary>
        /// Gets the default value for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public static object? Default(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if a type is numeric.  Nullable numeric types are considered numeric.
        /// </summary>
        /// <returns>
        /// True if the type is numeric; otherwise false.
        /// </returns>
        /// <remarks>
        /// Boolean is not considered numeric.
        /// </remarks>
        public static bool IsNumeric(Type type)
        {
            if (type == null)
            {
                return false;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                return IsNumeric(underlyingType);
            }
            else
            {
                return NumericTypes.Contains(type);
            }
        }

        [Flags]
        private enum OperatorType
        {
            Implicit = 1,
            Explicit = 2
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static MethodInfo? FindTypeConversionOperatorMethod(Type fromType, Type toType, OperatorType operatorType)
        {
            const string implicitName = "op_Implicit";
            const string explicitName = "op_Explicit";

            bool allowImplicit = (operatorType & OperatorType.Implicit) == OperatorType.Implicit;
            bool allowExplicit = (operatorType & OperatorType.Explicit) == OperatorType.Explicit;

            foreach (MethodInfo method in fromType.GetMethods())
            {
                if (!method.IsSpecialName || method.ReturnType != toType)
                {
                    continue;
                }

                if (allowImplicit && method.Name == implicitName)
                {
                    return method;
                }

                if (allowExplicit && method.Name == explicitName)
                {
                    return method;
                }
            }

            return null;
        }
    }
}