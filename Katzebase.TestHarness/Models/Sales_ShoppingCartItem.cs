namespace Katzebase.TestHarness.Models
{
    public partial class Sales_ShoppingCartItem
    {
        #region Properties
        private int _shoppingCartItemID;
        public int ShoppingCartItemID
        {
            get
            {
                return this._shoppingCartItemID;
            }
            set
            {
                if (this._shoppingCartItemID != value)
                {
                    this._shoppingCartItemID = value;
                }
            }
        }
        private string? _shoppingCartID;
        public string? ShoppingCartID
        {
            get
            {
                return this._shoppingCartID;
            }
            set
            {
                if (this._shoppingCartID != value)
                {
                    this._shoppingCartID = value;
                }
            }
        }
        private int _quantity;
        public int Quantity
        {
            get
            {
                return this._quantity;
            }
            set
            {
                if (this._quantity != value)
                {
                    this._quantity = value;
                }
            }
        }
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
        private DateTime _dateCreated;
        public DateTime DateCreated
        {
            get
            {
                return this._dateCreated;
            }
            set
            {
                if (this._dateCreated != value)
                {
                    this._dateCreated = value;
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
