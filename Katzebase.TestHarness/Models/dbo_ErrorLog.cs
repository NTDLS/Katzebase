using System;
using System.Runtime.Serialization;

namespace Katzebase.TestHarness.Models
{
	public partial class dbo_ErrorLog
	{
		#region Properties
		private int _errorLogID;
		public int ErrorLogID
		{
			get
			{
				return this._errorLogID;
			}
			set
			{
				if (this._errorLogID != value)
				{
					this._errorLogID = value;
				}            
			}
		}
		private DateTime _errorTime;
		public DateTime ErrorTime
		{
			get
			{
				return this._errorTime;
			}
			set
			{
				if (this._errorTime != value)
				{
					this._errorTime = value;
				}            
			}
		}
		private string _userName;
		public string UserName
		{
			get
			{
				return this._userName;
			}
			set
			{
				if (this._userName != value)
				{
					this._userName = value;
				}            
			}
		}
		private int _errorNumber;
		public int ErrorNumber
		{
			get
			{
				return this._errorNumber;
			}
			set
			{
				if (this._errorNumber != value)
				{
					this._errorNumber = value;
				}            
			}
		}
		private int? _errorSeverity;
		public int? ErrorSeverity
		{
			get
			{
				return this._errorSeverity;
			}
			set
			{
				if (this._errorSeverity != value)
				{
					this._errorSeverity = value;
				}            
			}
		}
		private int? _errorState;
		public int? ErrorState
		{
			get
			{
				return this._errorState;
			}
			set
			{
				if (this._errorState != value)
				{
					this._errorState = value;
				}            
			}
		}
		private string _errorProcedure;
		public string ErrorProcedure
		{
			get
			{
				return this._errorProcedure;
			}
			set
			{
				if (this._errorProcedure != value)
				{
					this._errorProcedure = value;
				}            
			}
		}
		private int? _errorLine;
		public int? ErrorLine
		{
			get
			{
				return this._errorLine;
			}
			set
			{
				if (this._errorLine != value)
				{
					this._errorLine = value;
				}            
			}
		}
		private string _errorMessage;
		public string ErrorMessage
		{
			get
			{
				return this._errorMessage;
			}
			set
			{
				if (this._errorMessage != value)
				{
					this._errorMessage = value;
				}            
			}
		}
			
		#endregion
	}
}
