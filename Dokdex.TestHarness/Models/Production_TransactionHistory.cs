using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Production_TransactionHistory
	{
		#region Properties
		private int _transactionID;
		public int TransactionID
		{
			get
			{
				return this._transactionID;
			}
			set
			{
				if (this._transactionID != value)
				{
					this._transactionID = value;
				}            
			}
		}
		private int _productID;
		public int ProductID
		{
			get
			{
				return this._productID;
			}
			set
			{
				if (this._productID != value)
				{
					this._productID = value;
				}            
			}
		}
		private int _referenceOrderID;
		public int ReferenceOrderID
		{
			get
			{
				return this._referenceOrderID;
			}
			set
			{
				if (this._referenceOrderID != value)
				{
					this._referenceOrderID = value;
				}            
			}
		}
		private int _referenceOrderLineID;
		public int ReferenceOrderLineID
		{
			get
			{
				return this._referenceOrderLineID;
			}
			set
			{
				if (this._referenceOrderLineID != value)
				{
					this._referenceOrderLineID = value;
				}            
			}
		}
		private DateTime _transactionDate;
		public DateTime TransactionDate
		{
			get
			{
				return this._transactionDate;
			}
			set
			{
				if (this._transactionDate != value)
				{
					this._transactionDate = value;
				}            
			}
		}
		private string _transactionType;
		public string TransactionType
		{
			get
			{
				return this._transactionType;
			}
			set
			{
				if (this._transactionType != value)
				{
					this._transactionType = value;
				}            
			}
		}
		private int _quantity;
		public int Quantity
		{
			get
			{
				return this._quantity;
			}
			set
			{
				if (this._quantity != value)
				{
					this._quantity = value;
				}            
			}
		}
		private Decimal _actualCost;
		public Decimal ActualCost
		{
			get
			{
				return this._actualCost;
			}
			set
			{
				if (this._actualCost != value)
				{
					this._actualCost = value;
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
