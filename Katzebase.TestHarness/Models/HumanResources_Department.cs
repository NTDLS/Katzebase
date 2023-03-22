namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_Department
    {
        #region Properties
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
        private string _groupName;
        public string GroupName
        {
            get
            {
                return this._groupName;
            }
            set
            {
                if (this._groupName != value)
                {
                    this._groupName = value;
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
