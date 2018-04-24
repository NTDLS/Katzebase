using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Sales_SalesTerritoryHistory
	{
		#region Properties
		private int _businessEntityID;
		public int BusinessEntityID
		{
			get
			{
				return this._businessEntityID;
			}
			set
			{
				if (this._businessEntityID != value)
				{
					this._businessEntityID = value;
				}            
			}
		}
		private int _territoryID;
		public int TerritoryID
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
