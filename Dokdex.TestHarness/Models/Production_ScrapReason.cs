using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Production_ScrapReason
	{
		#region Properties
		private short _scrapReasonID;
		public short ScrapReasonID
		{
			get
			{
				return this._scrapReasonID;
			}
			set
			{
				if (this._scrapReasonID != value)
				{
					this._scrapReasonID = value;
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
