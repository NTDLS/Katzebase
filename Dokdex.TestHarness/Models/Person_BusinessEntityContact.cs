using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Person_BusinessEntityContact
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
		private int _personID;
		public int PersonID
		{
			get
			{
				return this._personID;
			}
			set
			{
				if (this._personID != value)
				{
					this._personID = value;
				}            
			}
		}
		private int _contactTypeID;
		public int ContactTypeID
		{
			get
			{
				return this._contactTypeID;
			}
			set
			{
				if (this._contactTypeID != value)
				{
					this._contactTypeID = value;
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
