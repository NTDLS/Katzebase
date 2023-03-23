namespace Katzebase.TestHarness.Models
{
    public partial class Person_PersonPhone
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
        private string? _phoneNumber;
        public string? PhoneNumber
        {
            get
            {
                return this._phoneNumber;
            }
            set
            {
                if (this._phoneNumber != value)
                {
                    this._phoneNumber = value;
                }
            }
        }
        private int _phoneNumberTypeID;
        public int PhoneNumberTypeID
        {
            get
            {
                return this._phoneNumberTypeID;
            }
            set
            {
                if (this._phoneNumberTypeID != value)
                {
                    this._phoneNumberTypeID = value;
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
