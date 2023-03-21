using System;
using System.Data;

namespace Katzebase.TestHarness
{
    public static class IDataRecordExtensions
    {
        #region Index Based
        public static DateTime? GetNullableDateTime(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetDateTime(index) : (DateTime?)null;
        }

        public static byte? GetNullableByte(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetByte(index) : (byte?)null;
        }

        public static decimal? GetNullableDecimal(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetDecimal(index) : (decimal?)null;
        }

        public static short? GetNullableInt16(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetInt16(index) : (short?)null;
        }

        public static int? GetNullableInt32(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetInt32(index) : (int?)null;
        }

        public static long? GetNullableInt64(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetInt64(index) : (long?)null;
        }

        public static bool? GetNullableBoolean(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetBoolean(index) : (bool?)null;
        }

        public static string GetNullableString(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetString(index) : (string)null;
        }

        public static byte[] GetNullableByteArray(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? (byte[])dataRecord.GetValue(index) : (byte[])null;
        }

        public static byte[] GetByteArray(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? (byte[])dataRecord.GetValue(index) : (byte[])null;
        }

        public static Guid? GetNullableGuid(this IDataRecord dataRecord, int index)
        {
            return dataRecord[index] != DBNull.Value ? dataRecord.GetGuid(index) : (Guid?)null;
        }
        #endregion

        #region Name Based
        public static DateTime GetDateTime(this IDataRecord dataRecord, string columnName)
        {
            return (DateTime)dataRecord[columnName];
        }

        public static DateTime? GetNullableDateTime(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (DateTime)dataRecord[columnName] : (DateTime?)null;
        }

        public static byte? GetNullableByte(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (byte?)dataRecord[columnName] : (byte?)null;
        }

        public static decimal? GetNullableDecimal(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (decimal)dataRecord[columnName] : (decimal?)null;
        }

        public static short? GetNullableInt16(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (short)dataRecord[columnName] : (short?)null;
        }

        public static int? GetNullableInt32(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (int)dataRecord[columnName] : (int?)null;
        }

        public static double? GetNullableDouble(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (double?)dataRecord[columnName] : (double?)null;
        }

        public static long? GetNullableInt64(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (long?)dataRecord[columnName] : (long?)null;
        }

        public static bool? GetNullableBoolean(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (bool)dataRecord[columnName] : (bool?)null;
        }

        public static bool GetBoolean(this IDataRecord dataRecord, string columnName)
        {
            return (bool)dataRecord[columnName];
        }

        public static string GetNullableString(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (string)dataRecord[columnName] : (string)null;
        }

        public static string GetNullableString(this IDataRecord dataRecord, string columnName, string defaultValue)
        {
            return dataRecord[columnName] != DBNull.Value ? (string)dataRecord[columnName] : (string)defaultValue;
        }

        public static Guid? GetNullableGuid(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (Guid?)dataRecord[columnName] : (Guid?)null;
        }

        public static byte[] GetNullableByteArray(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (byte[])dataRecord[columnName] : (byte[])null;
        }

        public static byte[] GetByteArray(this IDataRecord dataRecord, string columnName)
        {
            return dataRecord[columnName] != DBNull.Value ? (byte[])dataRecord[columnName] : (byte[])null;
        }
        #endregion
    }
}
