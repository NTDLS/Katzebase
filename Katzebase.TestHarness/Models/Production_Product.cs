namespace Katzebase.TestHarness.Models
{
    public partial class Production_Product
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
        private string _productNumber;
        public string ProductNumber
        {
            get
            {
                return this._productNumber;
            }
            set
            {
                if (this._productNumber != value)
                {
                    this._productNumber = value;
                }
            }
        }
        private bool _makeFlag;
        public bool MakeFlag
        {
            get
            {
                return this._makeFlag;
            }
            set
            {
                if (this._makeFlag != value)
                {
                    this._makeFlag = value;
                }
            }
        }
        private bool _finishedGoodsFlag;
        public bool FinishedGoodsFlag
        {
            get
            {
                return this._finishedGoodsFlag;
            }
            set
            {
                if (this._finishedGoodsFlag != value)
                {
                    this._finishedGoodsFlag = value;
                }
            }
        }
        private string _color;
        public string Color
        {
            get
            {
                return this._color;
            }
            set
            {
                if (this._color != value)
                {
                    this._color = value;
                }
            }
        }
        private short _safetyStockLevel;
        public short SafetyStockLevel
        {
            get
            {
                return this._safetyStockLevel;
            }
            set
            {
                if (this._safetyStockLevel != value)
                {
                    this._safetyStockLevel = value;
                }
            }
        }
        private short _reorderPoint;
        public short ReorderPoint
        {
            get
            {
                return this._reorderPoint;
            }
            set
            {
                if (this._reorderPoint != value)
                {
                    this._reorderPoint = value;
                }
            }
        }
        private Decimal _standardCost;
        public Decimal StandardCost
        {
            get
            {
                return this._standardCost;
            }
            set
            {
                if (this._standardCost != value)
                {
                    this._standardCost = value;
                }
            }
        }
        private Decimal _listPrice;
        public Decimal ListPrice
        {
            get
            {
                return this._listPrice;
            }
            set
            {
                if (this._listPrice != value)
                {
                    this._listPrice = value;
                }
            }
        }
        private string _size;
        public string Size
        {
            get
            {
                return this._size;
            }
            set
            {
                if (this._size != value)
                {
                    this._size = value;
                }
            }
        }
        private string _sizeUnitMeasureCode;
        public string SizeUnitMeasureCode
        {
            get
            {
                return this._sizeUnitMeasureCode;
            }
            set
            {
                if (this._sizeUnitMeasureCode != value)
                {
                    this._sizeUnitMeasureCode = value;
                }
            }
        }
        private string _weightUnitMeasureCode;
        public string WeightUnitMeasureCode
        {
            get
            {
                return this._weightUnitMeasureCode;
            }
            set
            {
                if (this._weightUnitMeasureCode != value)
                {
                    this._weightUnitMeasureCode = value;
                }
            }
        }
        private decimal? _weight;
        public decimal? Weight
        {
            get
            {
                return this._weight;
            }
            set
            {
                if (this._weight != value)
                {
                    this._weight = value;
                }
            }
        }
        private int _daysToManufacture;
        public int DaysToManufacture
        {
            get
            {
                return this._daysToManufacture;
            }
            set
            {
                if (this._daysToManufacture != value)
                {
                    this._daysToManufacture = value;
                }
            }
        }
        private string _productLine;
        public string ProductLine
        {
            get
            {
                return this._productLine;
            }
            set
            {
                if (this._productLine != value)
                {
                    this._productLine = value;
                }
            }
        }
        private string _class;
        public string Class
        {
            get
            {
                return this._class;
            }
            set
            {
                if (this._class != value)
                {
                    this._class = value;
                }
            }
        }
        private string _style;
        public string Style
        {
            get
            {
                return this._style;
            }
            set
            {
                if (this._style != value)
                {
                    this._style = value;
                }
            }
        }
        private int? _productSubcategoryID;
        public int? ProductSubcategoryID
        {
            get
            {
                return this._productSubcategoryID;
            }
            set
            {
                if (this._productSubcategoryID != value)
                {
                    this._productSubcategoryID = value;
                }
            }
        }
        private int? _productModelID;
        public int? ProductModelID
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
        private DateTime _sellStartDate;
        public DateTime SellStartDate
        {
            get
            {
                return this._sellStartDate;
            }
            set
            {
                if (this._sellStartDate != value)
                {
                    this._sellStartDate = value;
                }
            }
        }
        private DateTime? _sellEndDate;
        public DateTime? SellEndDate
        {
            get
            {
                return this._sellEndDate;
            }
            set
            {
                if (this._sellEndDate != value)
                {
                    this._sellEndDate = value;
                }
            }
        }
        private DateTime? _discontinuedDate;
        public DateTime? DiscontinuedDate
        {
            get
            {
                return this._discontinuedDate;
            }
            set
            {
                if (this._discontinuedDate != value)
                {
                    this._discontinuedDate = value;
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
