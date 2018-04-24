using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Production_UnitMeasure
	{
		#region Properties
		private string _unitMeasureCode;
		public string UnitMeasureCode
		{
			get
			{
				return this._unitMeasureCode;
			}
			set
			{
				if (this._unitMeasureCode != value)
				{
					this._unitMeasureCode = value;
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
