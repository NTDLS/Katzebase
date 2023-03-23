namespace Katzebase.TestHarness.Models
{
    public partial class Production_Culture
    {
        #region Properties
        private string? _cultureID;
        public string? CultureID
        {
            get
            {
                return this._cultureID;
            }
            set
            {
                if (this._cultureID != value)
                {
                    this._cultureID = value;
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
