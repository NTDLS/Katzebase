﻿using NTDLS.Katzebase.Types;

namespace NTDLS.Katzebase.Payloads
{
    public class KbProcedureParameters
    {
        public KbInsensitiveDictionary<string> Collection { get; private set; } = new();

        public int Count => Collection.Count;

        public void Add(string name, string value)
        {
            if (name.StartsWith("@") == false)
            {
                name = "@" + name;
            }

            Collection.Add(name.ToLower(), value);
        }
    }
}
