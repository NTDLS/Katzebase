using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SpecialOfferProduct
    {
        #region Properties
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
