using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Person_CountryRegion
	{
		#region Properties
		private string _countryRegionCode;
		public string CountryRegionCode
		{
			get
			{
				return this._countryRegionCode;
			}
			set
			{
				if (this._countryRegionCode != value)
				{
					this._countryRegionCode = value;
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
