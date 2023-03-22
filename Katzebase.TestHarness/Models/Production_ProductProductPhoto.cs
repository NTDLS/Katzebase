namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductProductPhoto
    {
        #region Properties
        private int _productID;
        public int ProductID
        {
            get
            {
                return this._productID;
            }
            set
            {
                if (this._productID != value)
                {
                    this._productID = value;
                }
            }
        }
        private int _productPhotoID;
        public int ProductPhotoID
        {
            get
            {
                return this._productPhotoID;
            }
            set
            {
                if (this._productPhotoID != value)
                {
                    this._productPhotoID = value;
                }
            }
        }
        private bool _primary;
        public bool Primary
        {
            get
            {
                return this._primary;
            }
            set
            {
                if (this._primary != value)
                {
                    this._primary = value;
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
