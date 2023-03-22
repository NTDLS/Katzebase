namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesOrderHeaderSalesReason
    {
        #region Properties
        private int _salesOrderID;
        public int SalesOrderID
        {
            get
            {
                return this._salesOrderID;
            }
            set
            {
                if (this._salesOrderID != value)
                {
                    this._salesOrderID = value;
                }
            }
        }
        private int _salesReasonID;
        public int SalesReasonID
        {
            get
            {
                return this._salesReasonID;
            }
            set
            {
                if (this._salesReasonID != value)
                {
                    this._salesReasonID = value;
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
