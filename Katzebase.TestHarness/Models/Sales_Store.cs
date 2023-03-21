using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Sales_Store
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
		private string _demographics;
		public string Demographics
		{
			get
			{
				return this._demographics;
			}
			set
			{
				if (this._demographics != value)
				{
					this._demographics = value;
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
