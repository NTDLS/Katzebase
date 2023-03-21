using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Person_Person
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
        private string _personType;
        public string PersonType
        {
            get
            {
                return this._personType;
            }
            set
            {
                if (this._personType != value)
                {
                    this._personType = value;
                }
            }
        }
        private bool _nameStyle;
        public bool NameStyle
        {
            get
            {
                return this._nameStyle;
            }
            set
            {
                if (this._nameStyle != value)
                {
                    this._nameStyle = value;
                }
            }
        }
        private string _title;
        public string Title
        {
            get
            {
                return this._title;
            }
            set
            {
                if (this._title != value)
                {
                    this._title = value;
                }
            }
        }
        private string _firstName;
        public string FirstName
        {
            get
            {
                return this._firstName;
            }
            set
            {
                if (this._firstName != value)
                {
                    this._firstName = value;
                }
            }
        }
        private string _middleName;
        public string MiddleName
        {
            get
            {
                return this._middleName;
            }
            set
            {
                if (this._middleName != value)
                {
                    this._middleName = value;
                }
            }
        }
        private string _lastName;
        public string LastName
        {
            get
            {
                return this._lastName;
            }
            set
            {
                if (this._lastName != value)
                {
                    this._lastName = value;
                }
            }
        }
        private string _suffix;
        public string Suffix
        {
            get
            {
                return this._suffix;
            }
            set
            {
                if (this._suffix != value)
                {
                    this._suffix = value;
                }
            }
        }
        private int _emailPromotion;
        public int EmailPromotion
        {
            get
            {
                return this._emailPromotion;
            }
            set
            {
                if (this._emailPromotion != value)
                {
                    this._emailPromotion = value;
                }
            }
        }
        private string _additionalContactInfo;
        public string AdditionalContactInfo
        {
            get
            {
                return this._additionalContactInfo;
            }
            set
            {
                if (this._additionalContactInfo != value)
                {
                    this._additionalContactInfo = value;
                }
            }
        }
        private string _demographics;
        public string Demographics
        {
            get
            {
                return this._demographics;
            }
            set
            {
                if (this._demographics != value)
                {
                    this._demographics = value;
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
