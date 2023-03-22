namespace Katzebase.TestHarness.Models
{
    public partial class dbo_AWBuildVersion
    {
        #region Properties
        private byte _systemInformationID;
        public byte SystemInformationID
        {
            get
            {
                return this._systemInformationID;
            }
            set
            {
                if (this._systemInformationID != value)
                {
                    this._systemInformationID = value;
                }
            }
        }
        private string _database_Version;
        public string Database_Version
        {
            get
            {
                return this._database_Version;
            }
            set
            {
                if (this._database_Version != value)
                {
                    this._database_Version = value;
                }
            }
        }
        private DateTime _versionDate;
        public DateTime VersionDate
        {
            get
            {
                return this._versionDate;
            }
            set
            {
                if (this._versionDate != value)
                {
                    this._versionDate = value;
                }
            }
        }
        private DateTime _modifiedDate;
        public DateTime ModifiedDate
        {
            get
            {
                return this._modifiedDate;
            }
            set
            {
                if (this._modifiedDate != value)
                {
                    this._modifiedDate = value;
                }
            }
        }

        #endregion
    }
}
