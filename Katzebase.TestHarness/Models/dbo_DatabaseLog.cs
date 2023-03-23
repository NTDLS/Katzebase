namespace Katzebase.TestHarness.Models
{
    public partial class dbo_DatabaseLog
    {
        #region Properties
        private int _databaseLogID;
        public int DatabaseLogID
        {
            get
            {
                return this._databaseLogID;
            }
            set
            {
                if (this._databaseLogID != value)
                {
                    this._databaseLogID = value;
                }
            }
        }
        private DateTime _postTime;
        public DateTime PostTime
        {
            get
            {
                return this._postTime;
            }
            set
            {
                if (this._postTime != value)
                {
                    this._postTime = value;
                }
            }
        }
        private string? _databaseUser;
        public string? DatabaseUser
        {
            get
            {
                return this._databaseUser;
            }
            set
            {
                if (this._databaseUser != value)
                {
                    this._databaseUser = value;
                }
            }
        }
        private string? _event;
        public string? Event
        {
            get
            {
                return this._event;
            }
            set
            {
                if (this._event != value)
                {
                    this._event = value;
                }
            }
        }
        private string? _schema;
        public string? Schema
        {
            get
            {
                return this._schema;
            }
            set
            {
                if (this._schema != value)
                {
                    this._schema = value;
                }
            }
        }
        private string? _object;
        public string? Object
        {
            get
            {
                return this._object;
            }
            set
            {
                if (this._object != value)
                {
                    this._object = value;
                }
            }
        }
        private string? _tSQL;
        public string? TSQL
        {
            get
            {
                return this._tSQL;
            }
            set
            {
                if (this._tSQL != value)
                {
                    this._tSQL = value;
                }
            }
        }
        private string? _xmlEvent;
        public string? XmlEvent
        {
            get
            {
                return this._xmlEvent;
            }
            set
            {
                if (this._xmlEvent != value)
                {
                    this._xmlEvent = value;
                }
            }
        }

        #endregion
    }
}
