﻿namespace NTDLS.Katzebase.Payloads
{
    public class KbActionResponseGuid : KbBaseActionResponse
    {
        public Guid Id { get; set; }

        public KbActionResponseGuid(Guid id)
        {
            Id = id;
        }

        public KbActionResponseGuid()
        {
        }
    }
}
