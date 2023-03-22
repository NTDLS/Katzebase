namespace Katzebase.TestHarness.Models
{
    public partial class Production_WorkOrder
    {
        #region Properties
        private int _workOrderID;
        public int WorkOrderID
        {
            get
            {
                return this._workOrderID;
            }
            set
            {
                if (this._workOrderID != value)
                {
                    this._workOrderID = value;
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
        private int _orderQty;
        public int OrderQty
        {
            get
            {
                return this._orderQty;
            }
            set
            {
                if (this._orderQty != value)
                {
                    this._orderQty = value;
                }
            }
        }
        private int _stockedQty;
        public int StockedQty
        {
            get
            {
                return this._stockedQty;
            }
            set
            {
                if (this._stockedQty != value)
                {
                    this._stockedQty = value;
                }
            }
        }
        private short _scrappedQty;
        public short ScrappedQty
        {
            get
            {
                return this._scrappedQty;
            }
            set
            {
                if (this._scrappedQty != value)
                {
                    this._scrappedQty = value;
                }
            }
        }
        private DateTime _startDate;
        public DateTime StartDate
        {
            get
            {
                return this._startDate;
            }
            set
            {
                if (this._startDate != value)
                {
                    this._startDate = value;
                }
            }
        }
        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get
            {
                return this._endDate;
            }
            set
            {
                if (this._endDate != value)
                {
                    this._endDate = value;
                }
            }
        }
        private DateTime _dueDate;
        public DateTime DueDate
        {
            get
            {
                return this._dueDate;
            }
            set
            {
                if (this._dueDate != value)
                {
                    this._dueDate = value;
                }
            }
        }
        private short? _scrapReasonID;
        public short? ScrapReasonID
        {
            get
            {
                return this._scrapReasonID;
            }
            set
            {
                if (this._scrapReasonID != value)
                {
                    this._scrapReasonID = value;
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
