using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Production_ProductDocument
    {
        #region Properties
        private int _id;
        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                if (this._id != value)
                {
                    this._id = value;
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
        private int? _documentId;
        public int? DocumentId
        {
            get
            {
                return this._documentId;
            }
            set
            {
                if (this._documentId != value)
                {
                    this._documentId = value;
                }
            }
        }

        #endregion
    }
}
