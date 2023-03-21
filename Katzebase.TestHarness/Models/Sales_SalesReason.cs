using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Sales_SalesReason
	{
		#region Properties
		private int _salesReasonID;
		public int SalesReasonID
		{
			get
			{
				return this._salesReasonID;
			}
			set
			{
				if (this._salesReasonID != value)
				{
					this._salesReasonID = value;
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
		private string _reasonType;
		public string ReasonType
		{
			get
			{
				return this._reasonType;
			}
			set
			{
				if (this._reasonType != value)
				{
					this._reasonType = value;
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
