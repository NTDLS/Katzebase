namespace Katzebase.TestHarness.Models
{
    public partial class Production_Location
    {
        #region Properties
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
        private string? _name;
        public string? Name
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
        private Decimal _costRate;
        public Decimal CostRate
        {
            get
            {
                return this._costRate;
            }
            set
            {
                if (this._costRate != value)
                {
                    this._costRate = value;
                }
            }
        }
        private decimal _availability;
        public decimal Availability
        {
            get
            {
                return this._availability;
            }
            set
            {
                if (this._availability != value)
                {
                    this._availability = value;
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
