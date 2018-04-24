using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Sales_CreditCard
	{
		#region Properties
		private int _creditCardID;
		public int CreditCardID
		{
			get
			{
				return this._creditCardID;
			}
			set
			{
				if (this._creditCardID != value)
				{
					this._creditCardID = value;
				}            
			}
		}
		private string _cardType;
		public string CardType
		{
			get
			{
				return this._cardType;
			}
			set
			{
				if (this._cardType != value)
				{
					this._cardType = value;
				}            
			}
		}
		private string _cardNumber;
		public string CardNumber
		{
			get
			{
				return this._cardNumber;
			}
			set
			{
				if (this._cardNumber != value)
				{
					this._cardNumber = value;
				}            
			}
		}
		private byte _expMonth;
		public byte ExpMonth
		{
			get
			{
				return this._expMonth;
			}
			set
			{
				if (this._expMonth != value)
				{
					this._expMonth = value;
				}            
			}
		}
		private short _expYear;
		public short ExpYear
		{
			get
			{
				return this._expYear;
			}
			set
			{
				if (this._expYear != value)
				{
					this._expYear = value;
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
