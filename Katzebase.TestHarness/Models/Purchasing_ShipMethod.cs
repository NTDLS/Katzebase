namespace Katzebase.TestHarness.Models
{
    public partial class Purchasing_ShipMethod
    {
        #region Properties
        private int _shipMethodID;
        public int ShipMethodID
        {
            get
            {
                return this._shipMethodID;
            }
            set
            {
                if (this._shipMethodID != value)
                {
                    this._shipMethodID = value;
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
        private Decimal _shipBase;
        public Decimal ShipBase
        {
            get
            {
                return this._shipBase;
            }
            set
            {
                if (this._shipBase != value)
                {
                    this._shipBase = value;
                }
            }
        }
        private Decimal _shipRate;
        public Decimal ShipRate
        {
            get
            {
                return this._shipRate;
            }
            set
            {
                if (this._shipRate != value)
                {
                    this._shipRate = value;
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
