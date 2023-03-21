using System;

namespace Katzebase.TestHarness.Models
{
    public partial class Production_Document
    {
        #region Properties
        private int _id;
        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                if (this._id != value)
                {
                    this._id = value;
                }
            }
        }
        private string _title;
        public string Title
        {
            get
            {
                return this._title;
            }
            set
            {
                if (this._title != value)
                {
                    this._title = value;
                }
            }
        }
        private int _owner;
        public int Owner
        {
            get
            {
                return this._owner;
            }
            set
            {
                if (this._owner != value)
                {
                    this._owner = value;
                }
            }
        }
        private bool _folderFlag;
        public bool FolderFlag
        {
            get
            {
                return this._folderFlag;
            }
            set
            {
                if (this._folderFlag != value)
                {
                    this._folderFlag = value;
                }
            }
        }
        private string _fileName;
        public string FileName
        {
            get
            {
                return this._fileName;
            }
            set
            {
                if (this._fileName != value)
                {
                    this._fileName = value;
                }
            }
        }
        private string _fileExtension;
        public string FileExtension
        {
            get
            {
                return this._fileExtension;
            }
            set
            {
                if (this._fileExtension != value)
                {
                    this._fileExtension = value;
                }
            }
        }
        private string _revision;
        public string Revision
        {
            get
            {
                return this._revision;
            }
            set
            {
                if (this._revision != value)
                {
                    this._revision = value;
                }
            }
        }
        private int _changeNumber;
        public int ChangeNumber
        {
            get
            {
                return this._changeNumber;
            }
            set
            {
                if (this._changeNumber != value)
                {
                    this._changeNumber = value;
                }
            }
        }
        private byte _status;
        public byte Status
        {
            get
            {
                return this._status;
            }
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                }
            }
        }
        private string _documentSummary;
        public string DocumentSummary
        {
            get
            {
                return this._documentSummary;
            }
            set
            {
                if (this._documentSummary != value)
                {
                    this._documentSummary = value;
                }
            }
        }
        private byte[] _document;
        public byte[] Document
        {
            get
            {
                return this._document;
            }
            set
            {
                if (this._document != value)
                {
                    this._document = value;
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
