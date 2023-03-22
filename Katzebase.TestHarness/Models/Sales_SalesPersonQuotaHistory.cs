namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesPersonQuotaHistory
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
        private DateTime _quotaDate;
        public DateTime QuotaDate
        {
            get
            {
                return this._quotaDate;
            }
            set
            {
                if (this._quotaDate != value)
                {
                    this._quotaDate = value;
                }
            }
        }
        private Decimal _salesQuota;
        public Decimal SalesQuota
        {
            get
            {
                return this._salesQuota;
            }
            set
            {
                if (this._salesQuota != value)
                {
                    this._salesQuota = value;
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
