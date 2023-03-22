namespace Katzebase.TestHarness.Models
{
    public partial class Person_StateProvince
    {
        #region Properties
        private int _stateProvinceID;
        public int StateProvinceID
        {
            get
            {
                return this._stateProvinceID;
            }
            set
            {
                if (this._stateProvinceID != value)
                {
                    this._stateProvinceID = value;
                }
            }
        }
        private string _stateProvinceCode;
        public string StateProvinceCode
        {
            get
            {
                return this._stateProvinceCode;
            }
            set
            {
                if (this._stateProvinceCode != value)
                {
                    this._stateProvinceCode = value;
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
        private bool _isOnlyStateProvinceFlag;
        public bool IsOnlyStateProvinceFlag
        {
            get
            {
                return this._isOnlyStateProvinceFlag;
            }
            set
            {
                if (this._isOnlyStateProvinceFlag != value)
                {
                    this._isOnlyStateProvinceFlag = value;
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
