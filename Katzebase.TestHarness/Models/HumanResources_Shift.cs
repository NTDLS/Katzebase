namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_Shift
    {
        #region Properties
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
        private TimeSpan _startTime;
        public TimeSpan StartTime
        {
            get
            {
                return this._startTime;
            }
            set
            {
                if (this._startTime != value)
                {
                    this._startTime = value;
                }
            }
        }
        private TimeSpan _endTime;
        public TimeSpan EndTime
        {
            get
            {
                return this._endTime;
            }
            set
            {
                if (this._endTime != value)
                {
                    this._endTime = value;
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
