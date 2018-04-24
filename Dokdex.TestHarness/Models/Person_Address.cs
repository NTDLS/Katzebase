using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Person_Address
	{
		#region Properties
		private int _addressID;
		public int AddressID
		{
			get
			{
				return this._addressID;
			}
			set
			{
				if (this._addressID != value)
				{
					this._addressID = value;
				}            
			}
		}
		private string _addressLine1;
		public string AddressLine1
		{
			get
			{
				return this._addressLine1;
			}
			set
			{
				if (this._addressLine1 != value)
				{
					this._addressLine1 = value;
				}            
			}
		}
		private string _addressLine2;
		public string AddressLine2
		{
			get
			{
				return this._addressLine2;
			}
			set
			{
				if (this._addressLine2 != value)
				{
					this._addressLine2 = value;
				}            
			}
		}
		private string _city;
		public string City
		{
			get
			{
				return this._city;
			}
			set
			{
				if (this._city != value)
				{
					this._city = value;
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
		private string _postalCode;
		public string PostalCode
		{
			get
			{
				return this._postalCode;
			}
			set
			{
				if (this._postalCode != value)
				{
					this._postalCode = value;
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
