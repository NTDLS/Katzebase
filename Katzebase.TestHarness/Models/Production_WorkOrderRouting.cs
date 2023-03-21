using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class Production_WorkOrderRouting
	{
		#region Properties
		private int _workOrderID;
		public int WorkOrderID
		{
			get
			{
				return this._workOrderID;
			}
			set
			{
				if (this._workOrderID != value)
				{
					this._workOrderID = value;
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
		private short _operationSequence;
		public short OperationSequence
		{
			get
			{
				return this._operationSequence;
			}
			set
			{
				if (this._operationSequence != value)
				{
					this._operationSequence = value;
				}            
			}
		}
		private short _locationID;
		public short LocationID
		{
			get
			{
				return this._locationID;
			}
			set
			{
				if (this._locationID != value)
				{
					this._locationID = value;
				}            
			}
		}
		private DateTime _scheduledStartDate;
		public DateTime ScheduledStartDate
		{
			get
			{
				return this._scheduledStartDate;
			}
			set
			{
				if (this._scheduledStartDate != value)
				{
					this._scheduledStartDate = value;
				}            
			}
		}
		private DateTime _scheduledEndDate;
		public DateTime ScheduledEndDate
		{
			get
			{
				return this._scheduledEndDate;
			}
			set
			{
				if (this._scheduledEndDate != value)
				{
					this._scheduledEndDate = value;
				}            
			}
		}
		private DateTime? _actualStartDate;
		public DateTime? ActualStartDate
		{
			get
			{
				return this._actualStartDate;
			}
			set
			{
				if (this._actualStartDate != value)
				{
					this._actualStartDate = value;
				}            
			}
		}
		private DateTime? _actualEndDate;
		public DateTime? ActualEndDate
		{
			get
			{
				return this._actualEndDate;
			}
			set
			{
				if (this._actualEndDate != value)
				{
					this._actualEndDate = value;
				}            
			}
		}
		private decimal? _actualResourceHrs;
		public decimal? ActualResourceHrs
		{
			get
			{
				return this._actualResourceHrs;
			}
			set
			{
				if (this._actualResourceHrs != value)
				{
					this._actualResourceHrs = value;
				}            
			}
		}
		private Decimal _plannedCost;
		public Decimal PlannedCost
		{
			get
			{
				return this._plannedCost;
			}
			set
			{
				if (this._plannedCost != value)
				{
					this._plannedCost = value;
				}            
			}
		}
		private Decimal? _actualCost;
		public Decimal? ActualCost
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
