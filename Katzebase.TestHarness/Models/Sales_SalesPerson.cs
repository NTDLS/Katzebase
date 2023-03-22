namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesPerson
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
        private Decimal? _salesQuota;
        public Decimal? SalesQuota
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
        private Decimal _bonus;
        public Decimal Bonus
        {
            get
            {
                return this._bonus;
            }
            set
            {
                if (this._bonus != value)
                {
                    this._bonus = value;
                }
            }
        }
        private Decimal _commissionPct;
        public Decimal CommissionPct
        {
            get
            {
                return this._commissionPct;
            }
            set
            {
                if (this._commissionPct != value)
                {
                    this._commissionPct = value;
                }
            }
        }
        private Decimal _salesYTD;
        public Decimal SalesYTD
        {
            get
            {
                return this._salesYTD;
            }
            set
            {
                if (this._salesYTD != value)
                {
                    this._salesYTD = value;
                }
            }
        }
        private Decimal _salesLastYear;
        public Decimal SalesLastYear
        {
            get
            {
                return this._salesLastYear;
            }
            set
            {
                if (this._salesLastYear != value)
                {
                    this._salesLastYear = value;
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
