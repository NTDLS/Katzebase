using System;
using System.Runtime.Serialization;

namespace Dokdex.TestHarness.Models
{
	public partial class Production_ProductReview
	{
		#region Properties
		private int _productReviewID;
		public int ProductReviewID
		{
			get
			{
				return this._productReviewID;
			}
			set
			{
				if (this._productReviewID != value)
				{
					this._productReviewID = value;
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
		private string _reviewerName;
		public string ReviewerName
		{
			get
			{
				return this._reviewerName;
			}
			set
			{
				if (this._reviewerName != value)
				{
					this._reviewerName = value;
				}            
			}
		}
		private DateTime _reviewDate;
		public DateTime ReviewDate
		{
			get
			{
				return this._reviewDate;
			}
			set
			{
				if (this._reviewDate != value)
				{
					this._reviewDate = value;
				}            
			}
		}
		private string _emailAddress;
		public string EmailAddress
		{
			get
			{
				return this._emailAddress;
			}
			set
			{
				if (this._emailAddress != value)
				{
					this._emailAddress = value;
				}            
			}
		}
		private int _rating;
		public int Rating
		{
			get
			{
				return this._rating;
			}
			set
			{
				if (this._rating != value)
				{
					this._rating = value;
				}            
			}
		}
		private string _comments;
		public string Comments
		{
			get
			{
				return this._comments;
			}
			set
			{
				if (this._comments != value)
				{
					this._comments = value;
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
