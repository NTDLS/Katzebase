using System;

namespace Katzebase.TestHarness.Models
{
    public partial class HumanResources_JobCandidate
    {
        #region Properties
        private int _jobCandidateID;
        public int JobCandidateID
        {
            get
            {
                return this._jobCandidateID;
            }
            set
            {
                if (this._jobCandidateID != value)
                {
                    this._jobCandidateID = value;
                }
            }
        }
        private int? _businessEntityID;
        public int? BusinessEntityID
        {
            get
            {
                return this._businessEntityID;
            }
            set
            {
                if (this._businessEntityID != value)
                {
                    this._businessEntityID = value;
                }
            }
        }
        private string _resume;
        public string Resume
        {
            get
            {
                return this._resume;
            }
            set
            {
                if (this._resume != value)
                {
                    this._resume = value;
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
