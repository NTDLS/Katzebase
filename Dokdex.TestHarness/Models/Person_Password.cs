using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Person_Password
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
		private string _passwordHash;
		public string PasswordHash
		{
			get
			{
				return this._passwordHash;
			}
			set
			{
				if (this._passwordHash != value)
				{
					this._passwordHash = value;
				}            
			}
		}
		private string _passwordSalt;
		public string PasswordSalt
		{
			get
			{
				return this._passwordSalt;
			}
			set
			{
				if (this._passwordSalt != value)
				{
					this._passwordSalt = value;
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
