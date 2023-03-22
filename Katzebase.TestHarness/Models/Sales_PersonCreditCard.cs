namespace Katzebase.TestHarness.Models
{
    public partial class Sales_PersonCreditCard
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
        private int _creditCardID;
        public int CreditCardID
        {
            get
            {
                return this._creditCardID;
            }
            set
            {
                if (this._creditCardID != value)
                {
                    this._creditCardID = value;
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
