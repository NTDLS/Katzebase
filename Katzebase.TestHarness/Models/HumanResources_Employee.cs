namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_Employee
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
        private string _nationalIDNumber;
        public string NationalIDNumber
        {
            get
            {
                return this._nationalIDNumber;
            }
            set
            {
                if (this._nationalIDNumber != value)
                {
                    this._nationalIDNumber = value;
                }
            }
        }
        private string _loginID;
        public string LoginID
        {
            get
            {
                return this._loginID;
            }
            set
            {
                if (this._loginID != value)
                {
                    this._loginID = value;
                }
            }
        }
        private string _jobTitle;
        public string JobTitle
        {
            get
            {
                return this._jobTitle;
            }
            set
            {
                if (this._jobTitle != value)
                {
                    this._jobTitle = value;
                }
            }
        }
        private DateTime _birthDate;
        public DateTime BirthDate
        {
            get
            {
                return this._birthDate;
            }
            set
            {
                if (this._birthDate != value)
                {
                    this._birthDate = value;
                }
            }
        }
        private string _maritalStatus;
        public string MaritalStatus
        {
            get
            {
                return this._maritalStatus;
            }
            set
            {
                if (this._maritalStatus != value)
                {
                    this._maritalStatus = value;
                }
            }
        }
        private string _gender;
        public string Gender
        {
            get
            {
                return this._gender;
            }
            set
            {
                if (this._gender != value)
                {
                    this._gender = value;
                }
            }
        }
        private DateTime _hireDate;
        public DateTime HireDate
        {
            get
            {
                return this._hireDate;
            }
            set
            {
                if (this._hireDate != value)
                {
                    this._hireDate = value;
                }
            }
        }
        private bool _salariedFlag;
        public bool SalariedFlag
        {
            get
            {
                return this._salariedFlag;
            }
            set
            {
                if (this._salariedFlag != value)
                {
                    this._salariedFlag = value;
                }
            }
        }
        private short _vacationHours;
        public short VacationHours
        {
            get
            {
                return this._vacationHours;
            }
            set
            {
                if (this._vacationHours != value)
                {
                    this._vacationHours = value;
                }
            }
        }
        private short _sickLeaveHours;
        public short SickLeaveHours
        {
            get
            {
                return this._sickLeaveHours;
            }
            set
            {
                if (this._sickLeaveHours != value)
                {
                    this._sickLeaveHours = value;
                }
            }
        }
        private bool _currentFlag;
        public bool CurrentFlag
        {
            get
            {
                return this._currentFlag;
            }
            set
            {
                if (this._currentFlag != value)
                {
                    this._currentFlag = value;
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
