namespace Katzebase.TestHarness.Models
{
    public partial class Person_BusinessEntityAddress
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
        private int _addressID;
        public int AddressID
        {
            get
            {
                return this._addressID;
            }
            set
            {
                if (this._addressID != value)
                {
                    this._addressID = value;
                }
            }
        }
        private int _addressTypeID;
        public int AddressTypeID
        {
            get
            {
                return this._addressTypeID;
            }
            set
            {
                if (this._addressTypeID != value)
                {
                    this._addressTypeID = value;
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
