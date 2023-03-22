namespace Katzebase.TestHarness.Models
{
    public partial class Person_ContactType
    {
        #region Properties
        private int _contactTypeID;
        public int ContactTypeID
        {
            get
            {
                return this._contactTypeID;
            }
            set
            {
                if (this._contactTypeID != value)
                {
                    this._contactTypeID = value;
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
