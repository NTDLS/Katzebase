using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Purchasing_PurchaseOrderHeader
    {
        #region Properties
        private int _purchaseOrderID;
        public int PurchaseOrderID
        {
            get
            {
                return this._purchaseOrderID;
            }
            set
            {
                if (this._purchaseOrderID != value)
                {
                    this._purchaseOrderID = value;
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
        private int _employeeID;
        public int EmployeeID
        {
            get
            {
                return this._employeeID;
            }
            set
            {
                if (this._employeeID != value)
                {
                    this._employeeID = value;
                }
            }
        }
        private int _vendorID;
        public int VendorID
        {
            get
            {
                return this._vendorID;
            }
            set
            {
                if (this._vendorID != value)
                {
                    this._vendorID = value;
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
