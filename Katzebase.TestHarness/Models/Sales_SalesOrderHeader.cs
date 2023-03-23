namespace Katzebase.TestHarness.Models
{
    public partial class Sales_SalesOrderHeader
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
        private byte _revisionNumber;
        public byte RevisionNumber
        {
            get
            {
                return this._revisionNumber;
            }
            set
            {
                if (this._revisionNumber != value)
                {
                    this._revisionNumber = value;
                }
            }
        }
        private DateTime _orderDate;
        public DateTime OrderDate
        {
            get
            {
                return this._orderDate;
            }
            set
            {
                if (this._orderDate != value)
                {
                    this._orderDate = value;
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
        private DateTime? _shipDate;
        public DateTime? ShipDate
        {
            get
            {
                return this._shipDate;
            }
            set
            {
                if (this._shipDate != value)
                {
                    this._shipDate = value;
                }
            }
        }
        private byte _status;
        public byte Status
        {
            get
            {
                return this._status;
            }
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                }
            }
        }
        private bool _onlineOrderFlag;
        public bool OnlineOrderFlag
        {
            get
            {
                return this._onlineOrderFlag;
            }
            set
            {
                if (this._onlineOrderFlag != value)
                {
                    this._onlineOrderFlag = value;
                }
            }
        }
        private string? _salesOrderNumber;
        public string? SalesOrderNumber
        {
            get
            {
                return this._salesOrderNumber;
            }
            set
            {
                if (this._salesOrderNumber != value)
                {
                    this._salesOrderNumber = value;
                }
            }
        }
        private string? _purchaseOrderNumber;
        public string? PurchaseOrderNumber
        {
            get
            {
                return this._purchaseOrderNumber;
            }
            set
            {
                if (this._purchaseOrderNumber != value)
                {
                    this._purchaseOrderNumber = value;
                }
            }
        }
        private string? _accountNumber;
        public string? AccountNumber
        {
            get
            {
                return this._accountNumber;
            }
            set
            {
                if (this._accountNumber != value)
                {
                    this._accountNumber = value;
                }
            }
        }
        private int _customerID;
        public int CustomerID
        {
            get
            {
                return this._customerID;
            }
            set
            {
                if (this._customerID != value)
                {
                    this._customerID = value;
                }
            }
        }
        private int? _salesPersonID;
        public int? SalesPersonID
        {
            get
            {
                return this._salesPersonID;
            }
            set
            {
                if (this._salesPersonID != value)
                {
                    this._salesPersonID = value;
                }
            }
        }
        private int? _territoryID;
        public int? TerritoryID
        {
            get
            {
                return this._territoryID;
            }
            set
            {
                if (this._territoryID != value)
                {
                    this._territoryID = value;
                }
            }
        }
        private int _billToAddressID;
        public int BillToAddressID
        {
            get
            {
                return this._billToAddressID;
            }
            set
            {
                if (this._billToAddressID != value)
                {
                    this._billToAddressID = value;
                }
            }
        }
        private int _shipToAddressID;
        public int ShipToAddressID
        {
            get
            {
                return this._shipToAddressID;
            }
            set
            {
                if (this._shipToAddressID != value)
                {
                    this._shipToAddressID = value;
                }
            }
        }
        private int _shipMethodID;
        public int ShipMethodID
        {
            get
            {
                return this._shipMethodID;
            }
            set
            {
                if (this._shipMethodID != value)
                {
                    this._shipMethodID = value;
                }
            }
        }
        private int? _creditCardID;
        public int? CreditCardID
        {
            get
            {
                return this._creditCardID;
            }
            set
            {
                if (this._creditCardID != value)
                {
                    this._creditCardID = value;
                }
            }
        }
        private string? _creditCardApprovalCode;
        public string? CreditCardApprovalCode
        {
            get
            {
                return this._creditCardApprovalCode;
            }
            set
            {
                if (this._creditCardApprovalCode != value)
                {
                    this._creditCardApprovalCode = value;
                }
            }
        }
        private int? _currencyRateID;
        public int? CurrencyRateID
        {
            get
            {
                return this._currencyRateID;
            }
            set
            {
                if (this._currencyRateID != value)
                {
                    this._currencyRateID = value;
                }
            }
        }
        private Decimal _subTotal;
        public Decimal SubTotal
        {
            get
            {
                return this._subTotal;
            }
            set
            {
                if (this._subTotal != value)
                {
                    this._subTotal = value;
                }
            }
        }
        private Decimal _taxAmt;
        public Decimal TaxAmt
        {
            get
            {
                return this._taxAmt;
            }
            set
            {
                if (this._taxAmt != value)
                {
                    this._taxAmt = value;
                }
            }
        }
        private Decimal _freight;
        public Decimal Freight
        {
            get
            {
                return this._freight;
            }
            set
            {
                if (this._freight != value)
                {
                    this._freight = value;
                }
            }
        }
        private Decimal _totalDue;
        public Decimal TotalDue
        {
            get
            {
                return this._totalDue;
            }
            set
            {
                if (this._totalDue != value)
                {
                    this._totalDue = value;
                }
            }
        }
        private string? _comment;
        public string? Comment
        {
            get
            {
                return this._comment;
            }
            set
            {
                if (this._comment != value)
                {
                    this._comment = value;
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
