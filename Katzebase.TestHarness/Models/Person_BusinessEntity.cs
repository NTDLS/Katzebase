using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Person_BusinessEntity
    {
        #region Properties
        private int _businessEntityID;
        public int BusinessEntityID
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
        private Guid _rowguid;
        public Guid rowguid
        {
            get
            {
                return this._rowguid;
            }
            set
            {
                if (this._rowguid != value)
                {
                    this._rowguid = value;
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
