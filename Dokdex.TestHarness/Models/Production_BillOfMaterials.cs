using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Production_BillOfMaterials
	{
		#region Properties
		private int _billOfMaterialsID;
		public int BillOfMaterialsID
		{
			get
			{
				return this._billOfMaterialsID;
			}
			set
			{
				if (this._billOfMaterialsID != value)
				{
					this._billOfMaterialsID = value;
				}            
			}
		}
		private int? _productAssemblyID;
		public int? ProductAssemblyID
		{
			get
			{
				return this._productAssemblyID;
			}
			set
			{
				if (this._productAssemblyID != value)
				{
					this._productAssemblyID = value;
				}            
			}
		}
		private int _componentID;
		public int ComponentID
		{
			get
			{
				return this._componentID;
			}
			set
			{
				if (this._componentID != value)
				{
					this._componentID = value;
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
		private string _unitMeasureCode;
		public string UnitMeasureCode
		{
			get
			{
				return this._unitMeasureCode;
			}
			set
			{
				if (this._unitMeasureCode != value)
				{
					this._unitMeasureCode = value;
				}            
			}
		}
		private short _bOMLevel;
		public short BOMLevel
		{
			get
			{
				return this._bOMLevel;
			}
			set
			{
				if (this._bOMLevel != value)
				{
					this._bOMLevel = value;
				}            
			}
		}
		private decimal _perAssemblyQty;
		public decimal PerAssemblyQty
		{
			get
			{
				return this._perAssemblyQty;
			}
			set
			{
				if (this._perAssemblyQty != value)
				{
					this._perAssemblyQty = value;
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
