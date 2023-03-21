using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Sales_Currency
    {
        #region Properties
        private string _currencyCode;
        public string CurrencyCode
        {
            get
            {
                return this._currencyCode;
            }
            set
            {
                if (this._currencyCode != value)
                {
                    this._currencyCode = value;
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
