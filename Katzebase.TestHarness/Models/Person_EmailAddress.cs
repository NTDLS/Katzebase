namespace Katzebase.TestHarness.Models
{
    public partial class Person_EmailAddress
    {
        #region Properties
        private int _businessEntityID;
        public int BusinessEntityID
        {
            get
            {
                return this._businessEntityID;
            }
            set
            {
                if (this._businessEntityID != value)
                {
                    this._businessEntityID = value;
                }
            }
        }
        private int _emailAddressID;
        public int EmailAddressID
        {
            get
            {
                return this._emailAddressID;
            }
            set
            {
                if (this._emailAddressID != value)
                {
                    this._emailAddressID = value;
                }
            }
        }
        private string _emailAddress;
        public string EmailAddress
        {
            get
            {
                return this._emailAddress;
            }
            set
            {
                if (this._emailAddress != value)
                {
                    this._emailAddress = value;
                }
            }
        }
        private Guid _rowguid;
        public Guid rowguid
        {
            get
            {
                return this._rowguid;
            }
            set
            {
                if (this._rowguid != value)
                {
                    this._rowguid = value;
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
