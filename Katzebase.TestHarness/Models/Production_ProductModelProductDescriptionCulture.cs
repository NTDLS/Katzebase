namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductModelProductDescriptionCulture
    {
        #region Properties
        private int _productModelID;
        public int ProductModelID
        {
            get
            {
                return this._productModelID;
            }
            set
            {
                if (this._productModelID != value)
                {
                    this._productModelID = value;
                }
            }
        }
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
        private string _cultureID;
        public string CultureID
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
