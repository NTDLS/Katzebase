using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Sales_SpecialOffer
	{
		#region Properties
		private int _specialOfferID;
		public int SpecialOfferID
		{
			get
			{
				return this._specialOfferID;
			}
			set
			{
				if (this._specialOfferID != value)
				{
					this._specialOfferID = value;
				}            
			}
		}
		private string _description;
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (this._description != value)
				{
					this._description = value;
				}            
			}
		}
		private Decimal _discountPct;
		public Decimal DiscountPct
		{
			get
			{
				return this._discountPct;
			}
			set
			{
				if (this._discountPct != value)
				{
					this._discountPct = value;
				}            
			}
		}
		private string _type;
		public string Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (this._type != value)
				{
					this._type = value;
				}            
			}
		}
		private string _category;
		public string Category
		{
			get
			{
				return this._category;
			}
			set
			{
				if (this._category != value)
				{
					this._category = value;
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
		private DateTime _endDate;
		public DateTime EndDate
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
		private int _minQty;
		public int MinQty
		{
			get
			{
				return this._minQty;
			}
			set
			{
				if (this._minQty != value)
				{
					this._minQty = value;
				}            
			}
		}
		private int? _maxQty;
		public int? MaxQty
		{
			get
			{
				return this._maxQty;
			}
			set
			{
				if (this._maxQty != value)
				{
					this._maxQty = value;
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
