namespace Katzebase.TestHarness.Models
{
    public partial class Purchasing_ProductVendor
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
        private int _averageLeadTime;
        public int AverageLeadTime
        {
            get
            {
                return this._averageLeadTime;
            }
            set
            {
                if (this._averageLeadTime != value)
                {
                    this._averageLeadTime = value;
                }
            }
        }
        private Decimal _standardPrice;
        public Decimal StandardPrice
        {
            get
            {
                return this._standardPrice;
            }
            set
            {
                if (this._standardPrice != value)
                {
                    this._standardPrice = value;
                }
            }
        }
        private Decimal? _lastReceiptCost;
        public Decimal? LastReceiptCost
        {
            get
            {
                return this._lastReceiptCost;
            }
            set
            {
                if (this._lastReceiptCost != value)
                {
                    this._lastReceiptCost = value;
                }
            }
        }
        private DateTime? _lastReceiptDate;
        public DateTime? LastReceiptDate
        {
            get
            {
                return this._lastReceiptDate;
            }
            set
            {
                if (this._lastReceiptDate != value)
                {
                    this._lastReceiptDate = value;
                }
            }
        }
        private int _minOrderQty;
        public int MinOrderQty
        {
            get
            {
                return this._minOrderQty;
            }
            set
            {
                if (this._minOrderQty != value)
                {
                    this._minOrderQty = value;
                }
            }
        }
        private int _maxOrderQty;
        public int MaxOrderQty
        {
            get
            {
                return this._maxOrderQty;
            }
            set
            {
                if (this._maxOrderQty != value)
                {
                    this._maxOrderQty = value;
                }
            }
        }
        private int? _onOrderQty;
        public int? OnOrderQty
        {
            get
            {
                return this._onOrderQty;
            }
            set
            {
                if (this._onOrderQty != value)
                {
                    this._onOrderQty = value;
                }
            }
        }
        private string _unitMeasureCode;
        public string UnitMeasureCode
        {
            get
            {
                return this._unitMeasureCode;
            }
            set
            {
                if (this._unitMeasureCode != value)
                {
                    this._unitMeasureCode = value;
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
