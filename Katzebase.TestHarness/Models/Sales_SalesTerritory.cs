using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesTerritory
    {
        #region Properties
        private int _territoryID;
        public int TerritoryID
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
        private string _countryRegionCode;
        public string CountryRegionCode
        {
            get
            {
                return this._countryRegionCode;
            }
            set
            {
                if (this._countryRegionCode != value)
                {
                    this._countryRegionCode = value;
                }
            }
        }
        private string _group;
        public string Group
        {
            get
            {
                return this._group;
            }
            set
            {
                if (this._group != value)
                {
                    this._group = value;
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
        private Decimal _costYTD;
        public Decimal CostYTD
        {
            get
            {
                return this._costYTD;
            }
            set
            {
                if (this._costYTD != value)
                {
                    this._costYTD = value;
                }
            }
        }
        private Decimal _costLastYear;
        public Decimal CostLastYear
        {
            get
            {
                return this._costLastYear;
            }
            set
            {
                if (this._costLastYear != value)
                {
                    this._costLastYear = value;
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
