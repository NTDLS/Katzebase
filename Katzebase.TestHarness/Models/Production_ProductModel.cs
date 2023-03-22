namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductModel
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
        private string _catalogDescription;
        public string CatalogDescription
        {
            get
            {
                return this._catalogDescription;
            }
            set
            {
                if (this._catalogDescription != value)
                {
                    this._catalogDescription = value;
                }
            }
        }
        private string _instructions;
        public string Instructions
        {
            get
            {
                return this._instructions;
            }
            set
            {
                if (this._instructions != value)
                {
                    this._instructions = value;
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
