namespace Katzebase.TestHarness.Models
{
    public partial class Sales_CurrencyRate
    {
        #region Properties
        private int _currencyRateID;
        public int CurrencyRateID
        {
            get
            {
                return this._currencyRateID;
            }
            set
            {
                if (this._currencyRateID != value)
                {
                    this._currencyRateID = value;
                }
            }
        }
        private DateTime _currencyRateDate;
        public DateTime CurrencyRateDate
        {
            get
            {
                return this._currencyRateDate;
            }
            set
            {
                if (this._currencyRateDate != value)
                {
                    this._currencyRateDate = value;
                }
            }
        }
        private string? _fromCurrencyCode;
        public string? FromCurrencyCode
        {
            get
            {
                return this._fromCurrencyCode;
            }
            set
            {
                if (this._fromCurrencyCode != value)
                {
                    this._fromCurrencyCode = value;
                }
            }
        }
        private string? _toCurrencyCode;
        public string? ToCurrencyCode
        {
            get
            {
                return this._toCurrencyCode;
            }
            set
            {
                if (this._toCurrencyCode != value)
                {
                    this._toCurrencyCode = value;
                }
            }
        }
        private Decimal _averageRate;
        public Decimal AverageRate
        {
            get
            {
                return this._averageRate;
            }
            set
            {
                if (this._averageRate != value)
                {
                    this._averageRate = value;
                }
            }
        }
        private Decimal _endOfDayRate;
        public Decimal EndOfDayRate
        {
            get
            {
                return this._endOfDayRate;
            }
            set
            {
                if (this._endOfDayRate != value)
                {
                    this._endOfDayRate = value;
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
