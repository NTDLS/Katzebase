namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductDescription
    {
        #region Properties
        private int _productDescriptionID;
        public int ProductDescriptionID
        {
            get
            {
                return this._productDescriptionID;
            }
            set
            {
                if (this._productDescriptionID != value)
                {
                    this._productDescriptionID = value;
                }
            }
        }
        private string _description;
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                if (this._description != value)
                {
                    this._description = value;
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
