using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Production_ProductModelIllustration
	{
		#region Properties
		private int _productModelID;
		public int ProductModelID
		{
			get
			{
				return this._productModelID;
			}
			set
			{
				if (this._productModelID != value)
				{
					this._productModelID = value;
				}            
			}
		}
		private int _illustrationID;
		public int IllustrationID
		{
			get
			{
				return this._illustrationID;
			}
			set
			{
				if (this._illustrationID != value)
				{
					this._illustrationID = value;
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
