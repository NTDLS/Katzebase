namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesOrderDetail
    {
        #region Properties
        private int _salesOrderID;
        public int SalesOrderID
        {
            get
            {
                return this._salesOrderID;
            }
            set
            {
                if (this._salesOrderID != value)
                {
                    this._salesOrderID = value;
                }
            }
        }
        private int _salesOrderDetailID;
        public int SalesOrderDetailID
        {
            get
            {
                return this._salesOrderDetailID;
            }
            set
            {
                if (this._salesOrderDetailID != value)
                {
                    this._salesOrderDetailID = value;
                }
            }
        }
        private string? _carrierTrackingNumber;
        public string? CarrierTrackingNumber
        {
            get
            {
                return this._carrierTrackingNumber;
            }
            set
            {
                if (this._carrierTrackingNumber != value)
                {
                    this._carrierTrackingNumber = value;
                }
            }
        }
        private short _orderQty;
        public short OrderQty
        {
            get
            {
                return this._orderQty;
            }
            set
            {
                if (this._orderQty != value)
                {
                    this._orderQty = value;
                }
            }
        }
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
        private int _specialOfferID;
        public int SpecialOfferID
        {
            get
            {
                return this._specialOfferID;
            }
            set
            {
                if (this._specialOfferID != value)
                {
                    this._specialOfferID = value;
                }
            }
        }
        private Decimal _unitPrice;
        public Decimal UnitPrice
        {
            get
            {
                return this._unitPrice;
            }
            set
            {
                if (this._unitPrice != value)
                {
                    this._unitPrice = value;
                }
            }
        }
        private Decimal _unitPriceDiscount;
        public Decimal UnitPriceDiscount
        {
            get
            {
                return this._unitPriceDiscount;
            }
            set
            {
                if (this._unitPriceDiscount != value)
                {
                    this._unitPriceDiscount = value;
                }
            }
        }
        private decimal _lineTotal;
        public decimal LineTotal
        {
            get
            {
                return this._lineTotal;
            }
            set
            {
                if (this._lineTotal != value)
                {
                    this._lineTotal = value;
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
