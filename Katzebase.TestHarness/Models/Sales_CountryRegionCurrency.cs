using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Sales_CountryRegionCurrency
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
		private string _currencyCode;
		public string CurrencyCode
		{
			get
			{
				return this._currencyCode;
			}
			set
			{
				if (this._currencyCode != value)
				{
					this._currencyCode = value;
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
