namespace Katzebase.TestHarness.Models
{
    public partial class Purchasing_Vendor
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
        private string _accountNumber;
        public string AccountNumber
        {
            get
            {
                return this._accountNumber;
            }
            set
            {
                if (this._accountNumber != value)
                {
                    this._accountNumber = value;
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
        private byte _creditRating;
        public byte CreditRating
        {
            get
            {
                return this._creditRating;
            }
            set
            {
                if (this._creditRating != value)
                {
                    this._creditRating = value;
                }
            }
        }
        private bool _preferredVendorStatus;
        public bool PreferredVendorStatus
        {
            get
            {
                return this._preferredVendorStatus;
            }
            set
            {
                if (this._preferredVendorStatus != value)
                {
                    this._preferredVendorStatus = value;
                }
            }
        }
        private bool _activeFlag;
        public bool ActiveFlag
        {
            get
            {
                return this._activeFlag;
            }
            set
            {
                if (this._activeFlag != value)
                {
                    this._activeFlag = value;
                }
            }
        }
        private string _purchasingWebServiceURL;
        public string PurchasingWebServiceURL
        {
            get
            {
                return this._purchasingWebServiceURL;
            }
            set
            {
                if (this._purchasingWebServiceURL != value)
                {
                    this._purchasingWebServiceURL = value;
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
