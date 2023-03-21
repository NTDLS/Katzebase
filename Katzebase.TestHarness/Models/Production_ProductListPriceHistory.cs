using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductListPriceHistory
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
        private DateTime _startDate;
        public DateTime StartDate
        {
            get
            {
                return this._startDate;
            }
            set
            {
                if (this._startDate != value)
                {
                    this._startDate = value;
                }
            }
        }
        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get
            {
                return this._endDate;
            }
            set
            {
                if (this._endDate != value)
                {
                    this._endDate = value;
                }
            }
        }
        private Decimal _listPrice;
        public Decimal ListPrice
        {
            get
            {
                return this._listPrice;
            }
            set
            {
                if (this._listPrice != value)
                {
                    this._listPrice = value;
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
