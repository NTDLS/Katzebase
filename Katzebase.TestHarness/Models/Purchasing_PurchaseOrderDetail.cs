using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Purchasing_PurchaseOrderDetail
	{
		#region Properties
		private int _purchaseOrderID;
		public int PurchaseOrderID
		{
			get
			{
				return this._purchaseOrderID;
			}
			set
			{
				if (this._purchaseOrderID != value)
				{
					this._purchaseOrderID = value;
				}            
			}
		}
		private int _purchaseOrderDetailID;
		public int PurchaseOrderDetailID
		{
			get
			{
				return this._purchaseOrderDetailID;
			}
			set
			{
				if (this._purchaseOrderDetailID != value)
				{
					this._purchaseOrderDetailID = value;
				}            
			}
		}
		private DateTime _dueDate;
		public DateTime DueDate
		{
			get
			{
				return this._dueDate;
			}
			set
			{
				if (this._dueDate != value)
				{
					this._dueDate = value;
				}            
			}
		}
		private short _orderQty;
		public short OrderQty
		{
			get
			{
				return this._orderQty;
			}
			set
			{
				if (this._orderQty != value)
				{
					this._orderQty = value;
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
		private Decimal _unitPrice;
		public Decimal UnitPrice
		{
			get
			{
				return this._unitPrice;
			}
			set
			{
				if (this._unitPrice != value)
				{
					this._unitPrice = value;
				}            
			}
		}
		private Decimal _lineTotal;
		public Decimal LineTotal
		{
			get
			{
				return this._lineTotal;
			}
			set
			{
				if (this._lineTotal != value)
				{
					this._lineTotal = value;
				}            
			}
		}
		private decimal _receivedQty;
		public decimal ReceivedQty
		{
			get
			{
				return this._receivedQty;
			}
			set
			{
				if (this._receivedQty != value)
				{
					this._receivedQty = value;
				}            
			}
		}
		private decimal _rejectedQty;
		public decimal RejectedQty
		{
			get
			{
				return this._rejectedQty;
			}
			set
			{
				if (this._rejectedQty != value)
				{
					this._rejectedQty = value;
				}            
			}
		}
		private decimal _stockedQty;
		public decimal StockedQty
		{
			get
			{
				return this._stockedQty;
			}
			set
			{
				if (this._stockedQty != value)
				{
					this._stockedQty = value;
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
