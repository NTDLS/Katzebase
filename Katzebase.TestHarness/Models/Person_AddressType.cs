namespace Katzebase.TestHarness.Models
{
    public partial class Person_AddressType
    {
        #region Properties
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
        private string _name;
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (this._name != value)
                {
                    this._name = value;
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
