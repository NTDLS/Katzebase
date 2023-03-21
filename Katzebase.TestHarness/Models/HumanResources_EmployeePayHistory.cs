using System;

namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_EmployeePayHistory
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
        private DateTime _rateChangeDate;
        public DateTime RateChangeDate
        {
            get
            {
                return this._rateChangeDate;
            }
            set
            {
                if (this._rateChangeDate != value)
                {
                    this._rateChangeDate = value;
                }
            }
        }
        private Decimal _rate;
        public Decimal Rate
        {
            get
            {
                return this._rate;
            }
            set
            {
                if (this._rate != value)
                {
                    this._rate = value;
                }
            }
        }
        private byte _payFrequency;
        public byte PayFrequency
        {
            get
            {
                return this._payFrequency;
            }
            set
            {
                if (this._payFrequency != value)
                {
                    this._payFrequency = value;
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
