using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Sales_SalesTaxRate
	{
		#region Properties
		private int _salesTaxRateID;
		public int SalesTaxRateID
		{
			get
			{
				return this._salesTaxRateID;
			}
			set
			{
				if (this._salesTaxRateID != value)
				{
					this._salesTaxRateID = value;
				}            
			}
		}
		private int _stateProvinceID;
		public int StateProvinceID
		{
			get
			{
				return this._stateProvinceID;
			}
			set
			{
				if (this._stateProvinceID != value)
				{
					this._stateProvinceID = value;
				}            
			}
		}
		private byte _taxType;
		public byte TaxType
		{
			get
			{
				return this._taxType;
			}
			set
			{
				if (this._taxType != value)
				{
					this._taxType = value;
				}            
			}
		}
		private Decimal _taxRate;
		public Decimal TaxRate
		{
			get
			{
				return this._taxRate;
			}
			set
			{
				if (this._taxRate != value)
				{
					this._taxRate = value;
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
