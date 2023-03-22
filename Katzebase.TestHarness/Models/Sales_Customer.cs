namespace Katzebase.TestHarness.Models
{
    public partial class Sales_Customer
    {
        #region Properties
        private int _customerID;
        public int CustomerID
        {
            get
            {
                return this._customerID;
            }
            set
            {
                if (this._customerID != value)
                {
                    this._customerID = value;
                }
            }
        }
        private int? _personID;
        public int? PersonID
        {
            get
            {
                return this._personID;
            }
            set
            {
                if (this._personID != value)
                {
                    this._personID = value;
                }
            }
        }
        private int? _storeID;
        public int? StoreID
        {
            get
            {
                return this._storeID;
            }
            set
            {
                if (this._storeID != value)
                {
                    this._storeID = value;
                }
            }
        }
        private int? _territoryID;
        public int? TerritoryID
        {
            get
            {
                return this._territoryID;
            }
            set
            {
                if (this._territoryID != value)
                {
                    this._territoryID = value;
                }
            }
        }
        private string _accountNumber;
        public string AccountNumber
        {
            get
            {
                return this._accountNumber;
            }
            set
            {
                if (this._accountNumber != value)
                {
                    this._accountNumber = value;
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
