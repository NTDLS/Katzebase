namespace Katzebase.TestHarness.Models
{
    public partial class Person_PhoneNumberType
    {
        #region Properties
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
