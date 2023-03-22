namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductInventory
    {
        #region Properties
        private int _productID;
        public int ProductID
        {
            get
            {
                return this._productID;
            }
            set
            {
                if (this._productID != value)
                {
                    this._productID = value;
                }
            }
        }
        private short _locationID;
        public short LocationID
        {
            get
            {
                return this._locationID;
            }
            set
            {
                if (this._locationID != value)
                {
                    this._locationID = value;
                }
            }
        }
        private string _shelf;
        public string Shelf
        {
            get
            {
                return this._shelf;
            }
            set
            {
                if (this._shelf != value)
                {
                    this._shelf = value;
                }
            }
        }
        private byte _bin;
        public byte Bin
        {
            get
            {
                return this._bin;
            }
            set
            {
                if (this._bin != value)
                {
                    this._bin = value;
                }
            }
        }
        private short _quantity;
        public short Quantity
        {
            get
            {
                return this._quantity;
            }
            set
            {
                if (this._quantity != value)
                {
                    this._quantity = value;
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
