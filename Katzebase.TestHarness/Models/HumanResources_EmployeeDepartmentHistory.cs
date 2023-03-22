namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_EmployeeDepartmentHistory
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
        private short _departmentID;
        public short DepartmentID
        {
            get
            {
                return this._departmentID;
            }
            set
            {
                if (this._departmentID != value)
                {
                    this._departmentID = value;
                }
            }
        }
        private byte _shiftID;
        public byte ShiftID
        {
            get
            {
                return this._shiftID;
            }
            set
            {
                if (this._shiftID != value)
                {
                    this._shiftID = value;
                }
            }
        }
        private DateTime _startDate;
        public DateTime StartDate
        {
            get
            {
                return this._startDate;
            }
            set
            {
                if (this._startDate != value)
                {
                    this._startDate = value;
                }
            }
        }
        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get
            {
                return this._endDate;
            }
            set
            {
                if (this._endDate != value)
                {
                    this._endDate = value;
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
